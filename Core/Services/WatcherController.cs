using Serilog;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;
using System.Threading.Channels;

namespace WsjtxWatcher.Core.Services;

public sealed class WatcherController : IWsjtEventSink, IDisposable
{
    private readonly ICountryCatalog _countryCatalog;
    private readonly IDeviceFeedbackService _deviceFeedbackService;
    private readonly DecodedMessageFactory _decodedMessageFactory;
    private readonly IGridCacheStore _gridCacheStore;
    private readonly INotificationService _notificationService;
    private readonly ISettingsStore _settingsStore;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IWsjtGateway _wsjtGateway;
    private readonly Channel<PendingDecodeWorkItem> _decodeQueue = Channel.CreateUnbounded<PendingDecodeWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly Dictionary<string, ClientSessionState> _clientSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sessionSync = new();
    private readonly Task _decodeProcessingTask;
    private readonly WatchdogTimer _watchdogTimer;
    private AppSettings _settings = new();
    private long _runId;

    public WatcherController(
        WatcherState state,
        ISettingsStore settingsStore,
        ICountryCatalog countryCatalog,
        IGridCacheStore gridCacheStore,
        INotificationService notificationService,
        IDeviceFeedbackService deviceFeedbackService,
        IUiDispatcher uiDispatcher,
        IWsjtGateway wsjtGateway,
        DecodedMessageFactory decodedMessageFactory)
    {
        State = state;
        _settingsStore = settingsStore;
        _countryCatalog = countryCatalog;
        _gridCacheStore = gridCacheStore;
        _notificationService = notificationService;
        _deviceFeedbackService = deviceFeedbackService;
        _uiDispatcher = uiDispatcher;
        _wsjtGateway = wsjtGateway;
        _decodedMessageFactory = decodedMessageFactory;
        _watchdogTimer = new WatchdogTimer(TimeSpan.FromSeconds(16), OnTimeoutChanged);
        _decodeProcessingTask = Task.Run(() => ProcessDecodeQueueAsync(_disposeCts.Token));
    }

    public WatcherState State { get; }

    public AppSettings CurrentSettings => _settings.Clone();

    public Task FeedTimeoutDogAsync()
    {
        _watchdogTimer.Feed();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        await _countryCatalog.EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReloadSettingsAsync(CancellationToken cancellationToken = default)
    {
        _settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            await _wsjtGateway.StopAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _runId);
            ResetRuntimeState();
            await _wsjtGateway.StartAsync(ParsePort(_settings.Port), this, cancellationToken).ConfigureAwait(false);
            _watchdogTimer.Start();

            await _uiDispatcher.InvokeAsync(() =>
            {
                State.IsServiceRunning = true;
                State.ResetConnection();
            }).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _watchdogTimer.Stop();
            Interlocked.Increment(ref _runId);
            DrainDecodeQueue();
            ResetRuntimeState();
            await _wsjtGateway.StopAsync(cancellationToken).ConfigureAwait(false);

            await _uiDispatcher.InvokeAsync(() =>
            {
                State.IsServiceRunning = false;
                State.ResetConnection();
            }).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        if (!State.IsServiceRunning)
        {
            await ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await StopAsync(cancellationToken).ConfigureAwait(false);
        await StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetCacheAsync(CancellationToken cancellationToken = default)
    {
        await _gridCacheStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await _uiDispatcher.InvokeAsync(State.ClearMessages).ConfigureAwait(false);
    }

    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        await _settingsStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await _gridCacheStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _uiDispatcher.InvokeAsync(State.ClearMessages).ConfigureAwait(false);
    }

    public async Task OnSessionActivityAsync(WsjtSessionEvent sessionEvent, CancellationToken cancellationToken = default)
    {
        GetOrCreateSession(sessionEvent.ClientId);
        await _uiDispatcher.InvokeAsync(() =>
        {
            State.ClientId = sessionEvent.ClientId;
            State.SessionEndPoint = sessionEvent.SessionEndPoint;
            State.IsWaitingForConnection = false;
            State.IsTimedOut = false;
        }).ConfigureAwait(false);
    }

    public async Task OnDecodeAsync(WsjtDecodeEvent decodeEvent, CancellationToken cancellationToken = default)
    {
        await OnSessionActivityAsync(decodeEvent, cancellationToken).ConfigureAwait(false);
        var workItem = new PendingDecodeWorkItem(
            decodeEvent,
            _settings.Clone(),
            GetDialFrequencyForClient(decodeEvent.ClientId),
            Interlocked.Read(ref _runId));
        await _decodeQueue.Writer.WriteAsync(workItem, cancellationToken).ConfigureAwait(false);
    }

    public async Task OnStatusAsync(WsjtStatusEvent statusEvent, CancellationToken cancellationToken = default)
    {
        await OnSessionActivityAsync(statusEvent, cancellationToken).ConfigureAwait(false);
        var session = GetOrCreateSession(statusEvent.ClientId);
        bool shouldAddTransmitMessage;
        string switchNotice;
        lock (_sessionSync)
        {
            shouldAddTransmitMessage = !string.IsNullOrWhiteSpace(statusEvent.TxMode)
                                       && !session.IsTransmitting
                                       && statusEvent.Transmitting;
            switchNotice = BuildStatusSwitchNotice(
                session.CurrentBand,
                statusEvent.DialFrequencyHz,
                session.CurrentMode,
                statusEvent.Mode,
                statusEvent.TxMode);
            session.DialFrequencyHz = statusEvent.DialFrequencyHz;
            session.IsTransmitting = statusEvent.Transmitting;
            session.TransmitMessage = statusEvent.TransmitMessage ?? string.Empty;
            session.CurrentBand = RadioBandUtility.GetBandName(statusEvent.DialFrequencyHz);
            session.CurrentMode = ResolveStatusMode(statusEvent.Mode, statusEvent.TxMode);
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            State.CurrentFrequencyHz = statusEvent.DialFrequencyHz;
        }).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(switchNotice))
        {
            await _uiDispatcher.InvokeAsync(() =>
            {
                State.AddMessage(DecodedRadioMessage.CreateSystemNotice(switchNotice));
            }).ConfigureAwait(false);
        }

        if (shouldAddTransmitMessage)
        {
            var transmitMessage = string.IsNullOrWhiteSpace(statusEvent.TransmitMessage)
                ? string.Empty
                : statusEvent.TransmitMessage;
            await _uiDispatcher.InvokeAsync(() =>
            {
                State.AddMessage(DecodedRadioMessage.CreateUserTransmit(transmitMessage, statusEvent.TxMode));
            }).ConfigureAwait(false);
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            State.IsTransmitting = statusEvent.Transmitting;
            State.TransmitMessage = statusEvent.Transmitting ? statusEvent.TransmitMessage ?? string.Empty : string.Empty;
        }).ConfigureAwait(false);
    }

    private async Task ProcessDecodeQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                PendingDecodeWorkItem firstItem;
                try
                {
                    firstItem = await _decodeQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var batch = await ReadBatchAsync(firstItem, cancellationToken).ConfigureAwait(false);
                await ProcessDecodeBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Log.Error(exception, "Decode processing loop stopped unexpectedly.");
        }
    }

    private async Task<IReadOnlyList<PendingDecodeWorkItem>> ReadBatchAsync(PendingDecodeWorkItem firstItem, CancellationToken cancellationToken)
    {
        var batch = new List<PendingDecodeWorkItem>(16)
        {
            firstItem
        };
        var delayTask = Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);

        while (batch.Count < 32)
        {
            while (_decodeQueue.Reader.TryRead(out var nextItem))
            {
                batch.Add(nextItem);
                if (batch.Count >= 32)
                {
                    return batch;
                }
            }

            var waitTask = _decodeQueue.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var completedTask = await Task.WhenAny(waitTask, delayTask).ConfigureAwait(false);
            if (completedTask == delayTask)
            {
                break;
            }

            if (!await waitTask.ConfigureAwait(false))
            {
                break;
            }
        }

        return batch;
    }

    private async Task ProcessDecodeBatchAsync(IReadOnlyList<PendingDecodeWorkItem> batch, CancellationToken cancellationToken)
    {
        var gridUpdates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in batch)
        {
            if (TryGetGridUpdate(item.DecodeEvent, out var callsign, out var gridSquare))
            {
                gridUpdates[callsign] = gridSquare;
            }
        }

        if (gridUpdates.Count > 0)
        {
            await _gridCacheStore.SaveManyAsync(gridUpdates, cancellationToken).ConfigureAwait(false);
        }

        var acceptedMessages = new List<(DecodedRadioMessage Message, AppSettings Settings)>(batch.Count);
        foreach (var item in batch)
        {
            var message = await _decodedMessageFactory
                .CreateAsync(item.DecodeEvent, item.SettingsSnapshot, item.DialFrequencyHz, cancellationToken)
                .ConfigureAwait(false);

            if (item.RunId == Interlocked.Read(ref _runId))
            {
                acceptedMessages.Add((message, item.SettingsSnapshot));
            }
        }

        if (acceptedMessages.Count == 0)
        {
            return;
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            foreach (var item in acceptedMessages)
            {
                State.AddMessage(item.Message);
            }
        }).ConfigureAwait(false);

        foreach (var item in acceptedMessages)
        {
            await NotifyForMessageAsync(item.Message, item.Settings, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task NotifyForMessageAsync(DecodedRadioMessage message, AppSettings settingsSnapshot, CancellationToken cancellationToken)
    {
        if (IgnoredCallsignMatcher.IsIgnored(message, settingsSnapshot.IgnoredCallsigns))
        {
            return;
        }

        if (settingsSnapshot.VibrateOnAnyMessage)
        {
            await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (settingsSnapshot.NotifyOnAnyMessage)
        {
            await _notificationService.ShowMessageAlertAsync(message.Message, cancellationToken).ConfigureAwait(false);
        }

        if (message.MatchesWatchedCallsignPattern)
        {
            if (settingsSnapshot.VibrateOnMyCall)
            {
                await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
            }

            if (settingsSnapshot.NotifyOnMyCall)
            {
                await _notificationService.ShowMessageAlertAsync(message.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        if (message.MatchesSelectedDxcc)
        {
            if (settingsSnapshot.VibrateOnSelectedDxcc)
            {
                await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
            }

            if (settingsSnapshot.NotifyOnSelectedDxcc)
            {
                await _notificationService.ShowMessageAlertAsync(message.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void OnTimeoutChanged(bool isTimedOut)
    {
        _ = _uiDispatcher.InvokeAsync(() =>
        {
            State.IsTimedOut = isTimedOut;
            State.IsWaitingForConnection = isTimedOut || string.IsNullOrWhiteSpace(State.ClientId);
        });
    }

    private static int ParsePort(string value)
    {
        return int.TryParse(value, out var port) && port is > 0 and < 65536
            ? port
            : throw new InvalidOperationException($"Invalid WSJT-X port: {value}");
    }

    private string BuildStatusSwitchNotice(
        string previousBand,
        double currentFrequencyHz,
        string previousMode,
        string rawMode,
        string rawTxMode)
    {
        var currentBand = RadioBandUtility.GetBandName(currentFrequencyHz);
        var currentMode = ResolveStatusMode(rawMode, rawTxMode);

        if (string.IsNullOrWhiteSpace(previousBand) && string.IsNullOrWhiteSpace(previousMode))
        {
            return string.Empty;
        }

        var bandChanged = !string.IsNullOrWhiteSpace(currentBand)
                          && !string.Equals(previousBand, currentBand, StringComparison.OrdinalIgnoreCase);
        var modeChanged = !string.IsNullOrWhiteSpace(currentMode)
                          && !string.Equals(previousMode, currentMode, StringComparison.OrdinalIgnoreCase);
        if (!bandChanged && !modeChanged)
        {
            return string.Empty;
        }

        var language = AppLanguageUtility.Parse(_settings.Language, AppLanguage.English);
        if (language == AppLanguage.SimplifiedChinese)
        {
            return (bandChanged, modeChanged) switch
            {
                (true, true) => $"切换到{currentBand}波段/{currentMode}模式",
                (true, false) => $"切换到{currentBand}波段",
                (false, true) => $"切换到{currentMode}模式",
                _ => string.Empty
            };
        }

        return (bandChanged, modeChanged) switch
        {
            (true, true) => $"Switched to {currentBand} band / {currentMode} mode",
            (true, false) => $"Switched to {currentBand} band",
            (false, true) => $"Switched to {currentMode} mode",
            _ => string.Empty
        };
    }

    private static string ResolveStatusMode(string rawMode, string rawTxMode)
    {
        var mode = !string.IsNullOrWhiteSpace(rawMode) ? rawMode : rawTxMode;
        return WsjtxMessageParser.DecodeModeNotationsToString(mode);
    }

    private static bool TryGetGridUpdate(WsjtDecodeEvent decodeEvent, out string callsign, out string gridSquare)
    {
        callsign = string.Empty;
        gridSquare = string.Empty;

        if (!string.IsNullOrWhiteSpace(decodeEvent.RemoteCallsign) && MaidenheadLocator.IsValid(decodeEvent.RemoteGrid))
        {
            callsign = WsjtxMessageParser.NormalizeCallsign(decodeEvent.RemoteCallsign);
            gridSquare = decodeEvent.RemoteGrid.Trim().ToUpperInvariant();
            return true;
        }

        var gridUpdate = WsjtxMessageParser.ExtractGridUpdate(decodeEvent.Message ?? string.Empty);
        if (gridUpdate is null)
        {
            return false;
        }

        callsign = gridUpdate.Value.Callsign;
        gridSquare = gridUpdate.Value.GridSquare;
        return true;
    }

    private void ResetRuntimeState()
    {
        lock (_sessionSync)
        {
            _clientSessions.Clear();
        }
    }

    private void DrainDecodeQueue()
    {
        while (_decodeQueue.Reader.TryRead(out _))
        {
        }
    }

    private ClientSessionState GetOrCreateSession(string clientId)
    {
        lock (_sessionSync)
        {
            if (!_clientSessions.TryGetValue(clientId, out var session))
            {
                session = new ClientSessionState();
                _clientSessions[clientId] = session;
            }

            session.LastActivityUtc = DateTimeOffset.UtcNow;
            return session;
        }
    }

    private double GetDialFrequencyForClient(string clientId)
    {
        lock (_sessionSync)
        {
            return _clientSessions.TryGetValue(clientId, out var session) ? session.DialFrequencyHz : 0d;
        }
    }

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to stop watcher controller during dispose.");
        }

        _disposeCts.Cancel();
        try
        {
            _decodeProcessingTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }

        _disposeCts.Dispose();
        _watchdogTimer.Dispose();
        _lifecycleLock.Dispose();
    }

    private sealed record PendingDecodeWorkItem(
        WsjtDecodeEvent DecodeEvent,
        AppSettings SettingsSnapshot,
        double DialFrequencyHz,
        long RunId);

    private sealed class ClientSessionState
    {
        public double DialFrequencyHz { get; set; }
        public bool IsTransmitting { get; set; }
        public string TransmitMessage { get; set; } = string.Empty;
        public string CurrentBand { get; set; } = string.Empty;
        public string CurrentMode { get; set; } = string.Empty;
        public DateTimeOffset LastActivityUtc { get; set; }
    }
}

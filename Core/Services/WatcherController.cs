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
        lock (_sessionSync)
        {
            shouldAddTransmitMessage = string.Equals(statusEvent.TxMode, "FT8", StringComparison.OrdinalIgnoreCase)
                                       && !session.IsTransmitting
                                       && statusEvent.Transmitting;
            session.DialFrequencyHz = statusEvent.DialFrequencyHz;
            session.IsTransmitting = statusEvent.Transmitting;
            session.TransmitMessage = statusEvent.TransmitMessage ?? string.Empty;
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            State.CurrentFrequencyHz = statusEvent.DialFrequencyHz;
        }).ConfigureAwait(false);

        if (shouldAddTransmitMessage)
        {
            var transmitMessage = string.IsNullOrWhiteSpace(statusEvent.TransmitMessage)
                ? string.Empty
                : statusEvent.TransmitMessage;
            await _uiDispatcher.InvokeAsync(() =>
            {
                State.AddMessage(DecodedRadioMessage.CreateUserTransmit(transmitMessage));
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
            var gridUpdate = WsjtxMessageParser.ExtractGridUpdate(item.DecodeEvent.Message ?? string.Empty);
            if (gridUpdate is not null)
            {
                gridUpdates[gridUpdate.Value.Callsign] = gridUpdate.Value.GridSquare;
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
        if (settingsSnapshot.VibrateOnAnyMessage)
        {
            await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (settingsSnapshot.NotifyOnAnyMessage)
        {
            await _notificationService.ShowMessageAlertAsync(message.Message, cancellationToken).ConfigureAwait(false);
        }

        if (message.ContainsMyCallsign)
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
        public DateTimeOffset LastActivityUtc { get; set; }
    }
}

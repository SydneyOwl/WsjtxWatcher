using Serilog;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;
using System.Threading.Channels;

namespace WsjtxWatcher.Core.Services;

public sealed class WatcherController : IWsjtEventSink, IDisposable
{
    private readonly ICountryCatalog _countryCatalog;
    private readonly AlertRuleCooldownGate _alertRuleCooldownGate;
    private readonly AlertRuleEvaluator _alertRuleEvaluator;
    private readonly IDeviceFeedbackService _deviceFeedbackService;
    private readonly DecodedMessageFactory _decodedMessageFactory;
    private readonly IGridCacheStore _gridCacheStore;
    private readonly IIgnoredCallsignStore _ignoredCallsignStore;
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
    private readonly SemaphoreSlim _ignoredCallsignMutationLock = new(1, 1);
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
        IIgnoredCallsignStore ignoredCallsignStore,
        INotificationService notificationService,
        IDeviceFeedbackService deviceFeedbackService,
        IUiDispatcher uiDispatcher,
        IWsjtGateway wsjtGateway,
        DecodedMessageFactory decodedMessageFactory,
        AlertRuleEvaluator alertRuleEvaluator,
        AlertRuleCooldownGate alertRuleCooldownGate)
    {
        State = state;
        _settingsStore = settingsStore;
        _countryCatalog = countryCatalog;
        _alertRuleCooldownGate = alertRuleCooldownGate;
        _alertRuleEvaluator = alertRuleEvaluator;
        _gridCacheStore = gridCacheStore;
        _ignoredCallsignStore = ignoredCallsignStore;
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
        await RefreshIgnoredMessagesAsync(cancellationToken).ConfigureAwait(false);
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
            await _wsjtGateway.StartAsync(_settings.Clone(), this, cancellationToken).ConfigureAwait(false);
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

    public async Task SwitchRelaySourceAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        var normalizedSourceName = sourceName?.Trim() ?? string.Empty;
        await _uiDispatcher.InvokeAsync(() =>
        {
            State.ClearMessages();
            State.ResetConnection();
        }).ConfigureAwait(false);
        await _wsjtGateway.SelectSourceAsync(normalizedSourceName, cancellationToken).ConfigureAwait(false);
    }

    public Task RefreshGatewayAsync(CancellationToken cancellationToken = default)
    {
        return _wsjtGateway.RefreshAsync(cancellationToken);
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
        await _ignoredCallsignStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await _gridCacheStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _uiDispatcher.InvokeAsync(State.ClearMessages).ConfigureAwait(false);
    }

#if DEBUG
    public Task AddTestDataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        var messages = CreateDebugMessages(now, 120);

        return _uiDispatcher.InvokeAsync(() =>
        {
            foreach (var message in messages)
            {
                State.AddMessage(message);
            }

            State.IsWaitingForConnection = false;
            State.IsTimedOut = false;
            State.CurrentFrequencyHz = 14_074_000d;
        });
    }

    private static IReadOnlyList<DecodedRadioMessage> CreateDebugMessages(DateTime now, int count)
    {
        var modes = new[] { "FT8", "FT4", "MSK144", "Q65", "JT65", "FST4" };
        var frequencies = new[] { 1_840_000d, 3_573_000d, 7_074_000d, 10_136_000d, 14_074_000d, 18_100_000d, 21_074_000d, 24_915_000d, 28_074_000d, 50_313_000d, 144_174_000d };
        var grids = new[] { "PM95", "OL63", "CM87", "IO91", "JO65", "QF56", "FN20", "OF78", "RE66", "PL05", "KO85" };
        var callsigns = new[]
        {
            ("JA1ABC", "Japan", "日本", 339),
            ("BD7XYZ", "China", "中国", 318),
            ("K6TEST", "United States", "美国", 291),
            ("G4AAA", "England", "英格兰", 223),
            ("DL1XYZ", "Germany", "德国", 230),
            ("VK2HAM", "Australia", "澳大利亚", 150),
            ("VE3FOX", "Canada", "加拿大", 1),
            ("ZL3DX", "New Zealand", "新西兰", 170),
            ("LU8RAD", "Argentina", "阿根廷", 100),
            ("PY2FT8", "Brazil", "巴西", 108),
            ("UA3CQ", "European Russia", "俄罗斯欧洲部分", 54)
        };

        var messages = new List<DecodedRadioMessage>(count + 2)
        {
            DecodedRadioMessage.CreateSystemNotice("Debug test data")
        };

        for (var index = 0; index < count; index++)
        {
            var mode = modes[index % modes.Length];
            var frequency = frequencies[index % frequencies.Length];
            var periodSeconds = ResolveDebugPeriodSeconds(mode);
            var transmitter = callsigns[index % callsigns.Length];
            var receiver = callsigns[(index + 3) % callsigns.Length];
            var grid = grids[index % grids.Length];
            var isCq = index % 5 == 0;
            var isLowConfidence = index % 17 == 0;
            var messageText = isCq
                ? $"CQ {transmitter.Item1} {grid}"
                : $"{receiver.Item1} {transmitter.Item1} {FormatDebugReport(index)}";

            messages.Add(new DecodedRadioMessage
            {
                DecodeTimeUtc = FormatDebugTime(now.AddSeconds(-index * periodSeconds)),
                Snr = -24 + index % 31,
                OffsetTimeSeconds = ((index % 11) - 5) / 10d,
                OffsetFrequencyHz = 300 + index * 37 % 2600,
                Mode = mode,
                Message = messageText,
                LowConfidence = isLowConfidence,
                OffAir = index % 29 == 0,
                Receiver = isCq ? "CQ" : receiver.Item1,
                Transmitter = transmitter.Item1,
                TransmitterGrid = grid,
                DistanceText = $"{350 + index * 113 % 13800} km",
                ToCountryEnglish = isCq ? string.Empty : receiver.Item2,
                ToCountryChinese = isCq ? string.Empty : receiver.Item3,
                FromCountryEnglish = transmitter.Item2,
                FromCountryChinese = transmitter.Item3,
                ToCountryId = isCq ? 0 : receiver.Item4,
                FromCountryId = transmitter.Item4,
                ContainsMyCallsign = index % 13 == 0,
                MatchesWatchedCallsignPattern = index % 19 == 0,
                MatchesSelectedDxcc = index % 11 == 0,
                DialFrequencyHz = frequency
            });
        }

        messages.Add(DecodedRadioMessage.CreateUserTransmit("BD7XYZ K6TEST RR73", "FT8"));
        return messages;
    }

    private static string FormatDebugTime(DateTime time)
    {
        return $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}";
    }

    private static int ResolveDebugPeriodSeconds(string mode)
    {
        return mode switch
        {
            "FT4" => 7,
            "MSK144" => 15,
            "Q65" => 30,
            "JT65" => 60,
            "FST4" => 120,
            _ => 15
        };
    }

    private static string FormatDebugReport(int index)
    {
        var report = -24 + index % 35;
        return report >= 0 ? $"+{report:D2}" : report.ToString("D2");
    }
#endif

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

    public async Task OnQsoLoggedAsync(WsjtQsoLoggedEvent qsoLoggedEvent, CancellationToken cancellationToken = default)
    {
        await NotifyForLoggedQsoAsync(qsoLoggedEvent, cancellationToken).ConfigureAwait(false);
        await TryAutoIgnoreLoggedQsoAsync(qsoLoggedEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessDecodeQueueAsync(CancellationToken cancellationToken)
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
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to read the next decode work item.");
                continue;
            }

            try
            {
                var batch = await ReadBatchAsync(firstItem, cancellationToken).ConfigureAwait(false);
                await ProcessDecodeBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to process a decode batch. Continuing with the next batch.");
            }
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
            try
            {
                await _gridCacheStore.SaveManyAsync(gridUpdates, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Log.Warning(exception, "Failed to persist decoded grid updates.");
            }
        }

        var acceptedMessages = new List<(DecodedRadioMessage Message, AppSettings Settings)>(batch.Count);
        foreach (var item in batch)
        {
            DecodedRadioMessage message;
            try
            {
                message = await _decodedMessageFactory
                    .CreateAsync(item.DecodeEvent, item.SettingsSnapshot, item.DialFrequencyHz, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Log.Warning(
                    exception,
                    "Failed to materialize decoded message for client {ClientId}.",
                    item.DecodeEvent.ClientId);
                continue;
            }

            if (item.RunId == Interlocked.Read(ref _runId))
            {
                acceptedMessages.Add((message, item.SettingsSnapshot));
            }
        }

        if (acceptedMessages.Count == 0)
        {
            return;
        }

        try
        {
            await ApplyIgnoredStatusesAsync(acceptedMessages, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Log.Warning(exception, "Failed to apply ignored-callsign state to a decode batch.");
        }

        try
        {
            await _uiDispatcher.InvokeAsync(() =>
            {
                foreach (var item in acceptedMessages)
                {
                    State.AddMessage(item.Message);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Log.Warning(exception, "Failed to publish decoded messages to watcher state.");
        }

        foreach (var item in acceptedMessages)
        {
            try
            {
                await NotifyForMessageAsync(item.Message, item.Settings, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Log.Warning(exception, "Failed to deliver notifications for a decoded message.");
            }
        }
    }

    private async Task NotifyForMessageAsync(DecodedRadioMessage message, AppSettings settingsSnapshot, CancellationToken cancellationToken)
    {
        if (message.IsIgnored)
        {
            return;
        }

        var triggerResults = _alertRuleEvaluator.Evaluate(message, settingsSnapshot);
        foreach (var trigger in triggerResults)
        {
            if (!_alertRuleCooldownGate.TryEnter(trigger.RuleId, trigger.CooldownSeconds))
            {
                continue;
            }

            if (trigger.Vibrate)
            {
                await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
            }

            if (trigger.SendNotification)
            {
                switch (trigger.Kind)
                {
                    case AlertRuleKind.LoggedQso:
                        break;
                    default:
                        await _notificationService.ShowMessageAlertAsync(trigger.Message, cancellationToken).ConfigureAwait(false);
                        break;
                }
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

    private async Task TryAutoIgnoreLoggedQsoAsync(WsjtQsoLoggedEvent qsoLoggedEvent, CancellationToken cancellationToken)
    {
        var settingsSnapshot = _settings.Clone();
        if (!settingsSnapshot.AutoIgnoreLoggedQso)
        {
            return;
        }

        var callsign = IgnoredCallsignMatcher.NormalizeCallsign(qsoLoggedEvent.DxCall);
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return;
        }

        var band = ResolveLoggedQsoBand(qsoLoggedEvent);
        if (string.IsNullOrWhiteSpace(band))
        {
            return;
        }

        await _ignoredCallsignMutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settings = _settings.Clone();
            if (!settings.AutoIgnoreLoggedQso)
            {
                return;
            }

            var ignoredEntry = new IgnoredCallsignEntry
            {
                Callsign = callsign,
                Band = band
            };
            var added = await _ignoredCallsignStore.AddAsync(ignoredEntry, cancellationToken).ConfigureAwait(false);
            if (!added)
            {
                return;
            }
        }
        finally
        {
            _ignoredCallsignMutationLock.Release();
        }

        await RefreshIgnoredMessagesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifyForLoggedQsoAsync(WsjtQsoLoggedEvent qsoLoggedEvent, CancellationToken cancellationToken)
    {
        var settingsSnapshot = _settings.Clone();
        var callsign = IgnoredCallsignMatcher.NormalizeCallsign(qsoLoggedEvent.DxCall);
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return;
        }

        var band = ResolveLoggedQsoBand(qsoLoggedEvent);
        var trigger = _alertRuleEvaluator.Evaluate(qsoLoggedEvent, settingsSnapshot, callsign, band);
        if (trigger is null)
        {
            return;
        }

        if (!_alertRuleCooldownGate.TryEnter(trigger.RuleId, trigger.CooldownSeconds))
        {
            return;
        }

        if (trigger.Vibrate)
        {
            await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (trigger.SendNotification)
        {
            await _notificationService.ShowQsoLoggedAlertAsync(callsign, band, cancellationToken).ConfigureAwait(false);
        }
    }

    private string ResolveLoggedQsoBand(WsjtQsoLoggedEvent qsoLoggedEvent)
    {
        var normalizedBand = IgnoredCallsignMatcher.NormalizeBand(qsoLoggedEvent.Band);
        if (!string.IsNullOrWhiteSpace(normalizedBand))
        {
            return normalizedBand;
        }

        var frequencyHz = qsoLoggedEvent.FrequencyHz > 0d
            ? qsoLoggedEvent.FrequencyHz
            : GetDialFrequencyForClient(qsoLoggedEvent.ClientId);
        return IgnoredCallsignMatcher.NormalizeBand(RadioBandUtility.GetBandName(frequencyHz));
    }

    private async Task ApplyIgnoredStatusesAsync(
        IReadOnlyList<(DecodedRadioMessage Message, AppSettings Settings)> acceptedMessages,
        CancellationToken cancellationToken)
    {
        var lookupKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in acceptedMessages)
        {
            IgnoredCallsignMatcher.AddLookupKeys(
                lookupKeys,
                item.Message,
                item.Settings.IgnoredCallsignMatchTarget);
        }

        var matchedKeys = lookupKeys.Count == 0
            ? EmptyLookupSet.Instance
            : await _ignoredCallsignStore.FindMatchesAsync(lookupKeys, cancellationToken).ConfigureAwait(false);

        foreach (var item in acceptedMessages)
        {
            item.Message.IsIgnored = IgnoredCallsignMatcher.IsIgnored(
                item.Message,
                matchedKeys,
                item.Settings.IgnoredCallsignMatchTarget);
        }
    }

    private async Task RefreshIgnoredMessagesAsync(CancellationToken cancellationToken)
    {
        var settingsSnapshot = _settings.Clone();
        DecodedRadioMessage[] messages = [];
        await _uiDispatcher.InvokeAsync(() =>
        {
            messages = State.Messages
                .Where(message => !message.IsSystemNotice && !message.IsUserTransmit)
                .ToArray();
        }).ConfigureAwait(false);

        if (messages.Length == 0)
        {
            return;
        }

        var lookupKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var message in messages)
        {
            IgnoredCallsignMatcher.AddLookupKeys(lookupKeys, message, settingsSnapshot.IgnoredCallsignMatchTarget);
        }

        var matchedKeys = lookupKeys.Count == 0
            ? EmptyLookupSet.Instance
            : await _ignoredCallsignStore.FindMatchesAsync(lookupKeys, cancellationToken).ConfigureAwait(false);

        await _uiDispatcher.InvokeAsync(() =>
        {
            foreach (var message in messages)
            {
                message.IsIgnored = IgnoredCallsignMatcher.IsIgnored(
                    message,
                    matchedKeys,
                    settingsSnapshot.IgnoredCallsignMatchTarget);
            }

            State.RefreshMessagePresentation();
        }).ConfigureAwait(false);
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
        _ignoredCallsignMutationLock.Dispose();
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

    private sealed class EmptyLookupSet : IReadOnlySet<string>
    {
        public static EmptyLookupSet Instance { get; } = new();

        public int Count => 0;

        public bool Contains(string item) => false;

        public IEnumerator<string> GetEnumerator()
        {
            yield break;
        }

        public bool IsProperSubsetOf(IEnumerable<string> other) => true;

        public bool IsProperSupersetOf(IEnumerable<string> other) => !other.Any();

        public bool IsSubsetOf(IEnumerable<string> other) => true;

        public bool IsSupersetOf(IEnumerable<string> other) => !other.Any();

        public bool Overlaps(IEnumerable<string> other) => false;

        public bool SetEquals(IEnumerable<string> other) => !other.Any();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

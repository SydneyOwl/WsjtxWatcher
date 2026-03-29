using Serilog;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

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
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly WatchdogTimer _watchdogTimer;
    private AppSettings _settings = new();
    private double _lastKnownDialFrequencyHz;
    private bool _lastTransmitState;

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
    }

    public WatcherState State { get; }

    public AppSettings CurrentSettings => _settings.Clone();

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
            await _wsjtGateway.StartAsync(ParsePort(_settings.Port), this, cancellationToken).ConfigureAwait(false);
            _watchdogTimer.Start();
            _lastKnownDialFrequencyHz = 0d;
            _lastTransmitState = false;

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
        _watchdogTimer.Feed();
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

        var message = await _decodedMessageFactory
            .CreateAsync(decodeEvent, _settings, _lastKnownDialFrequencyHz, cancellationToken)
            .ConfigureAwait(false);

        await _uiDispatcher.InvokeAsync(() =>
        {
            State.AddMessage(message);
        }).ConfigureAwait(false);

        await NotifyForMessageAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task OnStatusAsync(WsjtStatusEvent statusEvent, CancellationToken cancellationToken = default)
    {
        await OnSessionActivityAsync(statusEvent, cancellationToken).ConfigureAwait(false);
        _lastKnownDialFrequencyHz = statusEvent.DialFrequencyHz;

        await _uiDispatcher.InvokeAsync(() =>
        {
            State.CurrentFrequencyHz = statusEvent.DialFrequencyHz;
        }).ConfigureAwait(false);

        if (!string.Equals(statusEvent.TxMode, "FT8", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_lastTransmitState && statusEvent.Transmitting)
        {
            var transmitMessage = string.IsNullOrWhiteSpace(statusEvent.TransmitMessage)
                ? string.Empty
                : statusEvent.TransmitMessage;
            await _uiDispatcher.InvokeAsync(() =>
            {
                State.AddMessage(DecodedRadioMessage.CreateUserTransmit(transmitMessage));
            }).ConfigureAwait(false);
        }

        _lastTransmitState = statusEvent.Transmitting;

        await _uiDispatcher.InvokeAsync(() =>
        {
            State.IsTransmitting = statusEvent.Transmitting;
            State.TransmitMessage = statusEvent.Transmitting ? statusEvent.TransmitMessage ?? string.Empty : string.Empty;
        }).ConfigureAwait(false);
    }

    private async Task NotifyForMessageAsync(DecodedRadioMessage message, CancellationToken cancellationToken)
    {
        if (_settings.VibrateOnAnyMessage)
        {
            await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_settings.NotifyOnAnyMessage)
        {
            await _notificationService.ShowMessageAlertAsync(message.Message, cancellationToken).ConfigureAwait(false);
        }

        if (message.ContainsMyCallsign)
        {
            if (_settings.VibrateOnMyCall)
            {
                await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_settings.NotifyOnMyCall)
            {
                await _notificationService.ShowMessageAlertAsync(message.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        if (message.MatchesSelectedDxcc)
        {
            if (_settings.VibrateOnSelectedDxcc)
            {
                await _deviceFeedbackService.VibrateAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_settings.NotifyOnSelectedDxcc)
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

        _watchdogTimer.Dispose();
        _lifecycleLock.Dispose();
    }
}

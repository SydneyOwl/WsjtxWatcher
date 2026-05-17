using CommunityToolkit.Mvvm.ComponentModel;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppLanguageService _appLanguageService;
    private readonly IAppInfoService _appInfoService;
    private readonly IBackgroundAccessService _backgroundAccessService;
    private readonly ICloudlogImportSettingsStore _cloudlogImportSettingsStore;
    private readonly IGridCacheStore _gridCacheStore;
    private readonly IIgnoredCallsignStore _ignoredCallsignStore;
    private readonly ILogFileService _logFileService;
    private readonly INetworkInfoService _networkInfoService;
    private readonly IRelayConnectionProbe _relayConnectionProbe;
    private readonly ISettingsStore _settingsStore;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Services.WatcherController _watcherController;
    private readonly RelayRuntimeState _relayRuntimeState;

    [ObservableProperty]
    private string port = "2237";

    [ObservableProperty]
    private DataSourceType selectedDataSourceType = DataSourceType.Udp;

    [ObservableProperty]
    private string relayServerUrl = string.Empty;

    [ObservableProperty]
    private string relaySharedSecret = string.Empty;

    [ObservableProperty]
    private string relayTenantId = string.Empty;

    [ObservableProperty]
    private string relayPreferredSourceName = string.Empty;

    [ObservableProperty]
    private string relayTrustedFingerprint = string.Empty;

    [ObservableProperty]
    private string myCallsign = string.Empty;

    [ObservableProperty]
    private string myGrid = string.Empty;

    [ObservableProperty]
    private AppLanguage selectedLanguage = AppLanguage.English;

    [ObservableProperty]
    private int ignoredCallsignCount;

    [ObservableProperty]
    private int selectedDxccCount;

    [ObservableProperty]
    private bool autoIgnoreLoggedQso = true;

    [ObservableProperty]
    private AppTheme selectedTheme = AppTheme.FollowSystem;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        ICloudlogImportSettingsStore cloudlogImportSettingsStore,
        IGridCacheStore gridCacheStore,
        IIgnoredCallsignStore ignoredCallsignStore,
        INetworkInfoService networkInfoService,
        IBackgroundAccessService backgroundAccessService,
        ILogFileService logFileService,
        IRelayConnectionProbe relayConnectionProbe,
        IAppInfoService appInfoService,
        IAppLanguageService appLanguageService,
        IUiDispatcher uiDispatcher,
        RelayRuntimeState relayRuntimeState,
        Services.WatcherController watcherController)
    {
        _settingsStore = settingsStore;
        _cloudlogImportSettingsStore = cloudlogImportSettingsStore;
        _gridCacheStore = gridCacheStore;
        _ignoredCallsignStore = ignoredCallsignStore;
        _networkInfoService = networkInfoService;
        _backgroundAccessService = backgroundAccessService;
        _logFileService = logFileService;
        _relayConnectionProbe = relayConnectionProbe;
        _appInfoService = appInfoService;
        _appLanguageService = appLanguageService;
        _uiDispatcher = uiDispatcher;
        _relayRuntimeState = relayRuntimeState;
        _watcherController = watcherController;
    }

    public string LocalIpAddress => _networkInfoService.IsWifiConnected() ? _networkInfoService.GetLocalIpAddress() : string.Empty;

    public string VersionName => _appInfoService.VersionName;

    public bool IsIgnoringBatteryOptimizations => _backgroundAccessService.IsIgnoringBatteryOptimizations();

    public AppLanguage CurrentAppLanguage => _appLanguageService.CurrentLanguage;

    public bool IsLanguageChangePending => SelectedLanguage != CurrentAppLanguage;

    public bool IsUdpSourceSelected => SelectedDataSourceType == DataSourceType.Udp;

    public bool IsRelaySourceSelected => SelectedDataSourceType == DataSourceType.Relay;

    public bool IsWatcherServiceRunning => _watcherController.State.IsServiceRunning;

    public RelayRuntimeState RelayRuntimeState => _relayRuntimeState;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settingsTask = _settingsStore.LoadAsync(cancellationToken);
        var ignoredCallsignCountTask = _ignoredCallsignStore.CountAsync(cancellationToken);
        await Task.WhenAll(settingsTask, ignoredCallsignCountTask).ConfigureAwait(false);
        var settings = await settingsTask.ConfigureAwait(false);
        SelectedDataSourceType = settings.DataSourceType;
        Port = settings.Port;
        RelayServerUrl = settings.RelayServerUrl;
        RelaySharedSecret = settings.RelaySharedSecret;
        RelayTenantId = settings.RelayTenantId;
        RelayPreferredSourceName = settings.RelayPreferredSourceName;
        RelayTrustedFingerprint = settings.RelayTrustedFingerprint;
        SelectedLanguage = _appLanguageService.ResolveConfiguredLanguage(settings.Language);
        MyCallsign = settings.MyCallsign;
        MyGrid = settings.MyGrid;
        SelectedDxccCount = settings.PreferredDxccIds.Count;
        IgnoredCallsignCount = await ignoredCallsignCountTask.ConfigureAwait(false);
        AutoIgnoreLoggedQso = settings.AutoIgnoreLoggedQso;
        SelectedTheme = settings.Theme;
        OnPropertyChanged(nameof(LocalIpAddress));
        OnPropertyChanged(nameof(IsIgnoringBatteryOptimizations));
        OnPropertyChanged(nameof(CurrentAppLanguage));
        OnPropertyChanged(nameof(IsLanguageChangePending));
        OnPropertyChanged(nameof(IsUdpSourceSelected));
        OnPropertyChanged(nameof(IsRelaySourceSelected));
    }

    public async Task<SettingsSaveResult> SaveAsync(CancellationToken cancellationToken = default)
    {
        var existingSettings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalizedSettings = CreateSettings(existingSettings);
        var restartRequired = RequiresGatewayRestart(existingSettings, normalizedSettings);
        var sourceSwitchRequired = RequiresRelaySourceSwitch(existingSettings, normalizedSettings);
        var serviceWasRunning = _watcherController.State.IsServiceRunning;

        await _settingsStore.SaveAsync(normalizedSettings, cancellationToken).ConfigureAwait(false);

        if (restartRequired)
        {
            await _watcherController.RestartAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
            if (sourceSwitchRequired)
            {
                await _watcherController.SwitchRelaySourceAsync(normalizedSettings.RelayPreferredSourceName, cancellationToken).ConfigureAwait(false);
            }
        }

        return new SettingsSaveResult(restartRequired, serviceWasRunning);
    }

    public async Task SaveAndStopAsync(CancellationToken cancellationToken = default)
    {
        var existingSettings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalizedSettings = CreateSettings(existingSettings);

        await _settingsStore.SaveAsync(normalizedSettings, cancellationToken).ConfigureAwait(false);
        await _watcherController.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetCacheAsync(CancellationToken cancellationToken = default)
    {
        await _gridCacheStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await _watcherController.ResetCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        await _watcherController.ResetAllAsync(cancellationToken).ConfigureAwait(false);
        await _cloudlogImportSettingsStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public void OpenLogFile()
    {
        _logFileService.OpenLogFile();
    }

    public void RequestIgnoreBatteryOptimizations()
    {
        _backgroundAccessService.RequestIgnoreBatteryOptimizations();
        OnPropertyChanged(nameof(IsIgnoringBatteryOptimizations));
    }

    public void OpenBackgroundSettings()
    {
        _backgroundAccessService.OpenBackgroundSettings();
    }

    public async Task RefreshRelaySourcesAsync(CancellationToken cancellationToken = default)
    {
        if (IsWatcherServiceRunning)
        {
            await _watcherController.RefreshGatewayAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var result = await _relayConnectionProbe
            .GetWatchSourceCatalogAsync(CreateRelayProbeOptions(), cancellationToken)
            .ConfigureAwait(false);

        if (result.Success
            && string.IsNullOrWhiteSpace(RelayTrustedFingerprint)
            && !string.IsNullOrWhiteSpace(result.ObservedFingerprint))
        {
            RelayTrustedFingerprint = result.ObservedFingerprint;
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            _relayRuntimeState.SetFingerprint(RelayTrustedFingerprint);
            _relayRuntimeState.SetConnectionState(connecting: false, connected: result.Success, status: result.Message);
            _relayRuntimeState.UpdateCatalog(result.Sources, result.CurrentSourceName);
            if (!result.Success)
            {
                _relayRuntimeState.LastNotice = result.Message;
            }
        }).ConfigureAwait(false);
    }

    public Task<RelayConnectionTestResult> TestRelayConnectionAsync(CancellationToken cancellationToken = default)
    {
        return _relayConnectionProbe.TestWatchConnectionAsync(CreateRelayProbeOptions(), cancellationToken);
    }

#if DEBUG
    public Task AddTestDataAsync(CancellationToken cancellationToken = default)
    {
        return _watcherController.AddTestDataAsync(cancellationToken);
    }
#endif

    private AppSettings CreateSettings(AppSettings existingSettings)
    {
        var settings = existingSettings.Clone();
        settings.DataSourceType = SelectedDataSourceType;
        settings.Port = NormalizePort(Port);
        settings.RelayServerUrl = NormalizeUrl(RelayServerUrl);
        settings.RelaySharedSecret = (RelaySharedSecret ?? string.Empty).Trim();
        settings.RelayTenantId = (RelayTenantId ?? string.Empty).Trim();
        settings.RelayPreferredSourceName = (RelayPreferredSourceName ?? string.Empty).Trim();
        settings.RelayTrustedFingerprint = (RelayTrustedFingerprint ?? string.Empty).Trim();
        settings.Language = SelectedLanguage.ToStorageValue();
        settings.MyCallsign = (MyCallsign ?? string.Empty).Trim().ToUpperInvariant();
        settings.MyGrid = (MyGrid ?? string.Empty).Trim().ToUpperInvariant();
        settings.AutoIgnoreLoggedQso = AutoIgnoreLoggedQso;
        settings.Theme = SelectedTheme;
        return settings;
    }

    private static string NormalizePort(string? value)
    {
        return int.TryParse(value, out var port) && port is > 0 and < 65536 ? port.ToString() : "2237";
    }

    private static string NormalizeUrl(string? value)
    {
        return (value ?? string.Empty).Trim().TrimEnd('/');
    }

    private RelayConnectionProbeOptions CreateRelayProbeOptions()
    {
        return new RelayConnectionProbeOptions(
            NormalizeUrl(RelayServerUrl),
            (RelaySharedSecret ?? string.Empty).Trim(),
            (RelayTenantId ?? string.Empty).Trim(),
            (RelayTrustedFingerprint ?? string.Empty).Trim());
    }

    private static bool RequiresGatewayRestart(AppSettings existingSettings, AppSettings normalizedSettings)
    {
        return existingSettings.DataSourceType != normalizedSettings.DataSourceType
               || !string.Equals(existingSettings.Port, normalizedSettings.Port, StringComparison.Ordinal)
               || !string.Equals(existingSettings.RelayServerUrl, normalizedSettings.RelayServerUrl, StringComparison.Ordinal)
               || !string.Equals(existingSettings.RelaySharedSecret, normalizedSettings.RelaySharedSecret, StringComparison.Ordinal)
               || !string.Equals(existingSettings.RelayTenantId, normalizedSettings.RelayTenantId, StringComparison.Ordinal)
               || !string.Equals(existingSettings.RelayTrustedFingerprint, normalizedSettings.RelayTrustedFingerprint, StringComparison.Ordinal);
    }

    private static bool RequiresRelaySourceSwitch(AppSettings existingSettings, AppSettings normalizedSettings)
    {
        return existingSettings.DataSourceType == DataSourceType.Relay
               && normalizedSettings.DataSourceType == DataSourceType.Relay
               && !string.Equals(existingSettings.RelayPreferredSourceName, normalizedSettings.RelayPreferredSourceName, StringComparison.Ordinal);
    }

    partial void OnSelectedLanguageChanged(AppLanguage value)
    {
        OnPropertyChanged(nameof(IsLanguageChangePending));
    }

    partial void OnSelectedDataSourceTypeChanged(DataSourceType value)
    {
        OnPropertyChanged(nameof(IsUdpSourceSelected));
        OnPropertyChanged(nameof(IsRelaySourceSelected));
    }
}

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
    private readonly ISettingsStore _settingsStore;
    private readonly Services.WatcherController _watcherController;
    [ObservableProperty]
    private bool notifyOnAnyMessage;

    [ObservableProperty]
    private bool notifyOnMyCall;

    [ObservableProperty]
    private bool notifyOnSelectedDxcc;

    [ObservableProperty]
    private bool notifyOnLoggedQso;

    [ObservableProperty]
    private bool vibrateOnLoggedQso;

    [ObservableProperty]
    private string port = "2237";

    [ObservableProperty]
    private string myCallsign = string.Empty;

    [ObservableProperty]
    private string myGrid = string.Empty;

    [ObservableProperty]
    private bool vibrateOnAnyMessage;

    [ObservableProperty]
    private bool vibrateOnMyCall;

    [ObservableProperty]
    private bool vibrateOnSelectedDxcc;

    [ObservableProperty]
    private int selectedDxccCount;

    [ObservableProperty]
    private AppLanguage selectedLanguage = AppLanguage.English;

    [ObservableProperty]
    private int watchedCallsignPatternCount;

    [ObservableProperty]
    private int ignoredCallsignCount;

    [ObservableProperty]
    private IgnoredCallsignMatchTarget ignoredCallsignMatchTarget = IgnoredCallsignMatchTarget.TransmitterOnly;

    [ObservableProperty]
    private WatchedCallsignMatchTarget watchedCallsignMatchTarget = WatchedCallsignMatchTarget.TransmitterOnly;

    [ObservableProperty]
    private SelectedDxccMatchTarget selectedDxccMatchTarget = SelectedDxccMatchTarget.TransmitterOnly;

    [ObservableProperty]
    private bool autoIgnoreLoggedQso = true;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        ICloudlogImportSettingsStore cloudlogImportSettingsStore,
        IGridCacheStore gridCacheStore,
        IIgnoredCallsignStore ignoredCallsignStore,
        INetworkInfoService networkInfoService,
        IBackgroundAccessService backgroundAccessService,
        ILogFileService logFileService,
        IAppInfoService appInfoService,
        IAppLanguageService appLanguageService,
        Services.WatcherController watcherController)
    {
        _settingsStore = settingsStore;
        _cloudlogImportSettingsStore = cloudlogImportSettingsStore;
        _gridCacheStore = gridCacheStore;
        _ignoredCallsignStore = ignoredCallsignStore;
        _networkInfoService = networkInfoService;
        _backgroundAccessService = backgroundAccessService;
        _logFileService = logFileService;
        _appInfoService = appInfoService;
        _appLanguageService = appLanguageService;
        _watcherController = watcherController;
    }

    public string LocalIpAddress => _networkInfoService.IsWifiConnected() ? _networkInfoService.GetLocalIpAddress() : string.Empty;

    public string VersionName => _appInfoService.VersionName;

    public bool IsIgnoringBatteryOptimizations => _backgroundAccessService.IsIgnoringBatteryOptimizations();

    public AppLanguage CurrentAppLanguage => _appLanguageService.CurrentLanguage;

    public bool IsLanguageChangePending => SelectedLanguage != CurrentAppLanguage;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settingsTask = _settingsStore.LoadAsync(cancellationToken);
        var ignoredCallsignCountTask = _ignoredCallsignStore.CountAsync(cancellationToken);
        await Task.WhenAll(settingsTask, ignoredCallsignCountTask).ConfigureAwait(false);
        var settings = await settingsTask.ConfigureAwait(false);
        Port = settings.Port;
        SelectedLanguage = _appLanguageService.ResolveConfiguredLanguage(settings.Language);
        MyCallsign = settings.MyCallsign;
        MyGrid = settings.MyGrid;
        WatchedCallsignPatternCount = settings.WatchedCallsignPatterns.Count;
        IgnoredCallsignCount = await ignoredCallsignCountTask.ConfigureAwait(false);
        NotifyOnMyCall = settings.NotifyOnMyCall;
        NotifyOnAnyMessage = settings.NotifyOnAnyMessage;
        NotifyOnSelectedDxcc = settings.NotifyOnSelectedDxcc;
        NotifyOnLoggedQso = settings.NotifyOnLoggedQso;
        VibrateOnMyCall = settings.VibrateOnMyCall;
        VibrateOnAnyMessage = settings.VibrateOnAnyMessage;
        VibrateOnSelectedDxcc = settings.VibrateOnSelectedDxcc;
        VibrateOnLoggedQso = settings.VibrateOnLoggedQso;
        IgnoredCallsignMatchTarget = settings.IgnoredCallsignMatchTarget;
        WatchedCallsignMatchTarget = settings.WatchedCallsignMatchTarget;
        SelectedDxccMatchTarget = settings.SelectedDxccMatchTarget;
        AutoIgnoreLoggedQso = settings.AutoIgnoreLoggedQso;
        SelectedDxccCount = settings.PreferredDxccIds.Count;
        OnPropertyChanged(nameof(LocalIpAddress));
        OnPropertyChanged(nameof(IsIgnoringBatteryOptimizations));
        OnPropertyChanged(nameof(CurrentAppLanguage));
        OnPropertyChanged(nameof(IsLanguageChangePending));
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var existingSettings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalizedSettings = CreateSettings(
            existingSettings.PreferredDxccIds,
            existingSettings.WatchedCallsignPatterns);
        var restartRequired = !string.Equals(existingSettings.Port, normalizedSettings.Port, StringComparison.Ordinal);

        await _settingsStore.SaveAsync(normalizedSettings, cancellationToken).ConfigureAwait(false);

        if (restartRequired)
        {
            await _watcherController.RestartAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SaveAndStopAsync(CancellationToken cancellationToken = default)
    {
        var existingSettings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var normalizedSettings = CreateSettings(
            existingSettings.PreferredDxccIds,
            existingSettings.WatchedCallsignPatterns);

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

    private AppSettings CreateSettings(
        IReadOnlyCollection<int> preferredDxccIds,
        IReadOnlyCollection<string> watchedCallsignPatterns)
    {
        return new AppSettings
        {
            Port = NormalizePort(Port),
            Language = SelectedLanguage.ToStorageValue(),
            MyCallsign = (MyCallsign ?? string.Empty).Trim().ToUpperInvariant(),
            MyGrid = (MyGrid ?? string.Empty).Trim().ToUpperInvariant(),
            WatchedCallsignPatterns = [.. CallsignPatternMatcher.NormalizePatterns(watchedCallsignPatterns)],
            NotifyOnMyCall = NotifyOnMyCall,
            NotifyOnAnyMessage = NotifyOnAnyMessage,
            NotifyOnSelectedDxcc = NotifyOnSelectedDxcc,
            NotifyOnLoggedQso = NotifyOnLoggedQso,
            VibrateOnMyCall = VibrateOnMyCall,
            VibrateOnAnyMessage = VibrateOnAnyMessage,
            VibrateOnSelectedDxcc = VibrateOnSelectedDxcc,
            VibrateOnLoggedQso = VibrateOnLoggedQso,
            AutoIgnoreLoggedQso = AutoIgnoreLoggedQso,
            IgnoredCallsignMatchTarget = IgnoredCallsignMatchTarget,
            WatchedCallsignMatchTarget = WatchedCallsignMatchTarget,
            SelectedDxccMatchTarget = SelectedDxccMatchTarget,
            PreferredDxccIds = new HashSet<int>(preferredDxccIds)
        };
    }

    private static string NormalizePort(string? value)
    {
        return int.TryParse(value, out var port) && port is > 0 and < 65536 ? port.ToString() : "2237";
    }

    partial void OnSelectedLanguageChanged(AppLanguage value)
    {
        OnPropertyChanged(nameof(IsLanguageChangePending));
    }
}

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

    public SettingsViewModel(
        ISettingsStore settingsStore,
        ICloudlogImportSettingsStore cloudlogImportSettingsStore,
        IGridCacheStore gridCacheStore,
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
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        Port = settings.Port;
        SelectedLanguage = _appLanguageService.ResolveConfiguredLanguage(settings.Language);
        MyCallsign = settings.MyCallsign;
        MyGrid = settings.MyGrid;
        WatchedCallsignPatternCount = settings.WatchedCallsignPatterns.Count;
        IgnoredCallsignCount = settings.IgnoredCallsigns.Count;
        NotifyOnMyCall = settings.NotifyOnMyCall;
        NotifyOnAnyMessage = settings.NotifyOnAnyMessage;
        NotifyOnSelectedDxcc = settings.NotifyOnSelectedDxcc;
        VibrateOnMyCall = settings.VibrateOnMyCall;
        VibrateOnAnyMessage = settings.VibrateOnAnyMessage;
        VibrateOnSelectedDxcc = settings.VibrateOnSelectedDxcc;
        IgnoredCallsignMatchTarget = settings.IgnoredCallsignMatchTarget;
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
            existingSettings.WatchedCallsignPatterns,
            existingSettings.IgnoredCallsigns);
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
        IReadOnlyCollection<string> watchedCallsignPatterns,
        IReadOnlyCollection<IgnoredCallsignEntry> ignoredCallsigns)
    {
        return new AppSettings
        {
            Port = NormalizePort(Port),
            Language = SelectedLanguage.ToStorageValue(),
            MyCallsign = (MyCallsign ?? string.Empty).Trim().ToUpperInvariant(),
            MyGrid = (MyGrid ?? string.Empty).Trim().ToUpperInvariant(),
            WatchedCallsignPatterns = [.. CallsignPatternMatcher.NormalizePatterns(watchedCallsignPatterns)],
            IgnoredCallsigns = [.. IgnoredCallsignMatcher.NormalizeEntries(ignoredCallsigns)],
            NotifyOnMyCall = NotifyOnMyCall,
            NotifyOnAnyMessage = NotifyOnAnyMessage,
            NotifyOnSelectedDxcc = NotifyOnSelectedDxcc,
            VibrateOnMyCall = VibrateOnMyCall,
            VibrateOnAnyMessage = VibrateOnAnyMessage,
            VibrateOnSelectedDxcc = VibrateOnSelectedDxcc,
            IgnoredCallsignMatchTarget = IgnoredCallsignMatchTarget,
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

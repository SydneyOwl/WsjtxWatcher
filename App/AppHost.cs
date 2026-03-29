using Android.App;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Services;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.Infrastructure.Persistence;
using WsjtxWatcher.Infrastructure.Platform;
using WsjtxWatcher.Infrastructure.Wsjt;

namespace WsjtxWatcher.App;

public sealed class AppHost
{
    private static readonly object SyncRoot = new();
    private static AppHost? _current;

    public static AppHost Current
    {
        get
        {
            lock (SyncRoot)
            {
                return _current ?? throw new InvalidOperationException("AppHost has not been initialized.");
            }
        }
    }

    public static void Initialize(Application application)
    {
        lock (SyncRoot)
        {
            _current ??= new AppHost(application);
        }
    }

    private AppHost(Application application)
    {
        var appInfo = new AndroidAppInfoService(application);
        var uiDispatcher = new AndroidUiDispatcher();
        var settingsStore = new AndroidSettingsStore(application, appInfo);
        var countryCatalog = new AssetCountryCatalog(application);
        var gridCacheStore = new JsonGridCacheStore(Path.Combine(appInfo.AppDataDirectory, "grid-cache.json"));
        var notificationService = new AndroidNotificationService(application);
        var deviceFeedbackService = new AndroidDeviceFeedbackService(application);
        var networkInfoService = new AndroidNetworkInfoService(application);
        var backgroundAccessService = new AndroidBackgroundAccessService(application);
        var logFileService = new AndroidLogFileService(application, appInfo);
        var gateway = new WsjtGateway();
        var messageFactory = new DecodedMessageFactory(countryCatalog, gridCacheStore);

        State = new WatcherState();
        WatcherController = new WatcherController(
            State,
            settingsStore,
            countryCatalog,
            gridCacheStore,
            notificationService,
            deviceFeedbackService,
            uiDispatcher,
            gateway,
            messageFactory);

        MainViewModel = new MainViewModel(WatcherController);
        SettingsViewModel = new SettingsViewModel(
            settingsStore,
            gridCacheStore,
            networkInfoService,
            backgroundAccessService,
            logFileService,
            appInfo,
            WatcherController);
        DxccSelectionViewModel = new DxccSelectionViewModel(countryCatalog, settingsStore);
    }

    public WatcherState State { get; }
    public WatcherController WatcherController { get; }
    public MainViewModel MainViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public DxccSelectionViewModel DxccSelectionViewModel { get; }
}

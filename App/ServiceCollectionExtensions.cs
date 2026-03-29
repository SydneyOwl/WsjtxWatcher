using Android.App;
using Microsoft.Extensions.DependencyInjection;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Services;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.Infrastructure.Persistence;
using WsjtxWatcher.Infrastructure.Platform;
using WsjtxWatcher.Infrastructure.Wsjt;

namespace WsjtxWatcher.App;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWsjtxWatcherServices(this IServiceCollection services, Application application)
    {
        services.AddSingleton(application);

        services.AddSingleton<WatcherState>();
        services.AddSingleton<IAppInfoService, AndroidAppInfoService>();
        services.AddSingleton<IUiDispatcher, AndroidUiDispatcher>();
        services.AddSingleton<ISettingsStore, AndroidSettingsStore>();
        services.AddSingleton<AndroidAppLanguageManager>();
        services.AddSingleton<IAppLanguageService>(serviceProvider => serviceProvider.GetRequiredService<AndroidAppLanguageManager>());
        services.AddSingleton<ICountryCatalog, AssetCountryCatalog>();
        services.AddSingleton<IGridCacheStore>(serviceProvider =>
        {
            var appInfo = serviceProvider.GetRequiredService<IAppInfoService>();
            return new JsonGridCacheStore(Path.Combine(appInfo.AppDataDirectory, "grid-cache.json"));
        });
        services.AddSingleton<INotificationService, AndroidNotificationService>();
        services.AddSingleton<IDeviceFeedbackService, AndroidDeviceFeedbackService>();
        services.AddSingleton<INetworkInfoService, AndroidNetworkInfoService>();
        services.AddSingleton<IBackgroundAccessService, AndroidBackgroundAccessService>();
        services.AddSingleton<ILogFileService, AndroidLogFileService>();
        services.AddSingleton<IWsjtGateway, WsjtGateway>();
        services.AddSingleton<DecodedMessageFactory>();
        services.AddSingleton<WatcherController>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<DxccSelectionViewModel>();
        return services;
    }
}

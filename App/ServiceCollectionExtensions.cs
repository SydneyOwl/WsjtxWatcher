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
        services.AddSingleton<RelayRuntimeState>();
        services.AddSingleton<IAppInfoService, AndroidAppInfoService>();
        services.AddSingleton<IUiDispatcher, AndroidUiDispatcher>();
        services.AddSingleton<ISettingsStore, AndroidSettingsStore>();
        services.AddSingleton<ICloudlogImportSettingsStore, AndroidCloudlogImportSettingsStore>();
        services.AddSingleton<AndroidAppLanguageManager>();
        services.AddSingleton<IAppLanguageService>(serviceProvider => serviceProvider.GetRequiredService<AndroidAppLanguageManager>());
        services.AddSingleton<IAppThemeService, AndroidAppThemeManager>();
        services.AddSingleton<WatcherDatabaseService>();
        services.AddSingleton<ICountryCatalog, SqliteCountryCatalog>();
        services.AddSingleton<IGridCacheStore, SqliteGridCacheStore>();
        services.AddSingleton<IIgnoredCallsignStore, SqliteIgnoredCallsignStore>();
        services.AddSingleton<INotificationService, AndroidNotificationService>();
        services.AddSingleton<IDeviceFeedbackService, AndroidDeviceFeedbackService>();
        services.AddSingleton<INetworkInfoService, AndroidNetworkInfoService>();
        services.AddSingleton<IBackgroundAccessService, AndroidBackgroundAccessService>();
        services.AddSingleton<ILogFileService, AndroidLogFileService>();
        services.AddSingleton<IRelayConnectionProbe, RelayConnectionProbe>();
        services.AddSingleton<UdpWsjtGateway>();
        services.AddSingleton<RelayWsjtGateway>();
        services.AddSingleton<IWsjtGateway, WsjtGateway>();
        services.AddSingleton<ICloudlogIgnoredCallsignImportService, CloudlogIgnoredCallsignImportService>();
        services.AddSingleton<RuleFieldValueResolver>();
        services.AddSingleton<RuleNamedSetResolver>();
        services.AddSingleton<AlertRuleEvaluator>();
        services.AddSingleton<AlertRuleCooldownGate>();
        services.AddSingleton<DecodedMessageFactory>();
        services.AddSingleton<WatcherController>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AlertRulesViewModel>();
        services.AddTransient<AlertRuleEditorViewModel>();
        services.AddTransient<RelaySourceSelectionViewModel>();
        services.AddSingleton<IgnoredCallsignViewModel>();
        return services;
    }
}

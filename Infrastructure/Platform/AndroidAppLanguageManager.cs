using System.Globalization;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Java.Util;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidAppLanguageManager : IAppLanguageService
{
    private readonly Application _application;
    private readonly ISettingsStore _settingsStore;
    private bool _isApplied;
    private AppLanguage _currentLanguage = AppLanguageUtility.DetectSystemLanguage();

    public AndroidAppLanguageManager(Application application, ISettingsStore settingsStore)
    {
        _application = application;
        _settingsStore = settingsStore;
    }

    public AppLanguage CurrentLanguage => _currentLanguage;

    public AppLanguage ResolveConfiguredLanguage(string? storedLanguageCode)
    {
        return AppLanguageUtility.Parse(storedLanguageCode, _currentLanguage);
    }

    public void ApplyCurrentLanguage()
    {
        var settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _currentLanguage = AppLanguageUtility.Parse(settings.Language, AppLanguageUtility.DetectSystemLanguage());
        ApplyLanguage(_application, _currentLanguage);
        _isApplied = true;
    }

    public Context CreateLocalizedContext(Context context)
    {
        if (!_isApplied)
        {
            ApplyCurrentLanguage();
        }

        return CreateLocalizedContextInternal(context, _currentLanguage);
    }

    private static void ApplyLanguage(Context context, AppLanguage language)
    {
        var localizedContext = CreateLocalizedContextInternal(context, language);
        var baseResources = context.Resources;
        var localizedResources = localizedContext.Resources;

        if (baseResources is not null && localizedResources is not null)
        {
#pragma warning disable CA1422
            baseResources.UpdateConfiguration(localizedResources.Configuration, localizedResources.DisplayMetrics);
#pragma warning restore CA1422
        }
    }

    private static Context CreateLocalizedContextInternal(Context context, AppLanguage language)
    {
        var locale = ToLocale(language);
        Locale.SetDefault(Locale.Category.Display!, locale);
        Locale.SetDefault(Locale.Category.Format!, locale);

        var culture = language.ToCultureInfo();
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        var existingConfiguration = context.Resources?.Configuration;
        var configuration = existingConfiguration is null ? new Configuration() : new Configuration(existingConfiguration);
        configuration.SetLocale(locale);
        configuration.SetLayoutDirection(locale);
        return context.CreateConfigurationContext(configuration) ?? context;
    }

    private static Locale ToLocale(AppLanguage language)
    {
        return language switch
        {
            AppLanguage.SimplifiedChinese => new Locale("zh", "CN"),
            _ => new Locale("en", "US")
        };
    }
}

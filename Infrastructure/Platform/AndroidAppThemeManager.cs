using Android.App;
using AndroidX.AppCompat.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidAppThemeManager : IAppThemeService
{
    private readonly ISettingsStore _settingsStore;
    private AppTheme _currentTheme;

    public AndroidAppThemeManager(Application application, ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        var settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _currentTheme = settings.Theme;
        ApplyThemeInternal(_currentTheme);
    }

    public AppTheme CurrentTheme => _currentTheme;

    public void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;
        ApplyThemeInternal(theme);
    }

    private static void ApplyThemeInternal(AppTheme theme)
    {
        var nightMode = theme switch
        {
            AppTheme.Light => AppCompatDelegate.ModeNightNo,
            AppTheme.Dark => AppCompatDelegate.ModeNightYes,
            _ => AppCompatDelegate.ModeNightFollowSystem
        };
        AppCompatDelegate.DefaultNightMode = nightMode;
    }
}

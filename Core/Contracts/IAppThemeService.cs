using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface IAppThemeService
{
    AppTheme CurrentTheme { get; }
    void ApplyTheme(AppTheme theme);
}

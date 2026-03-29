using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface IAppLanguageService
{
    AppLanguage CurrentLanguage { get; }
    AppLanguage ResolveConfiguredLanguage(string? storedLanguageCode);
}

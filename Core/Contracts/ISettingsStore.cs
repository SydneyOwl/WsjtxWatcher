using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}

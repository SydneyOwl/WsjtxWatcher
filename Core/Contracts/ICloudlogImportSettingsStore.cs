using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface ICloudlogImportSettingsStore
{
    Task<CloudlogImportSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CloudlogImportSettings settings, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}

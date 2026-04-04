using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface IIgnoredCallsignStore
{
    Task<IReadOnlyList<IgnoredCallsignEntry>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IEnumerable<IgnoredCallsignEntry> entries, CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}

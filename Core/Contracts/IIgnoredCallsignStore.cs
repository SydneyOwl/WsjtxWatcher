using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface IIgnoredCallsignStore
{
    Task<IReadOnlyList<IgnoredCallsignEntry>> LoadAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<bool> AddAsync(IgnoredCallsignEntry entry, CancellationToken cancellationToken = default);

    Task<IgnoredCallsignMergeResult> MergeAsync(
        IEnumerable<IgnoredCallsignEntry> entries,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(IgnoredCallsignEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> FindMatchesAsync(
        IEnumerable<string> lookupKeys,
        CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}

using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class IgnoredCallsignViewModel
{
    private readonly IIgnoredCallsignStore _ignoredCallsignStore;
    private readonly Services.WatcherController _watcherController;

    public IgnoredCallsignViewModel(
        IIgnoredCallsignStore ignoredCallsignStore,
        Services.WatcherController watcherController)
    {
        _ignoredCallsignStore = ignoredCallsignStore;
        _watcherController = watcherController;
    }

    public async Task<IReadOnlyList<IgnoredCallsignEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await _ignoredCallsignStore.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AddAsync(string callsign, string band, CancellationToken cancellationToken = default)
    {
        var added = await _ignoredCallsignStore.AddAsync(new IgnoredCallsignEntry
        {
            Callsign = IgnoredCallsignMatcher.NormalizeCallsign(callsign),
            Band = IgnoredCallsignMatcher.NormalizeBand(band)
        }, cancellationToken).ConfigureAwait(false);

        if (!added)
        {
            return false;
        }

        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        return added;
    }

    public async Task<IgnoredCallsignMergeResult> MergeAsync(
        IEnumerable<IgnoredCallsignEntry> importedEntries,
        CancellationToken cancellationToken = default)
    {
        var result = await _ignoredCallsignStore.MergeAsync(importedEntries, cancellationToken).ConfigureAwait(false);
        if (result.AddedCount > 0)
        {
            await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task RemoveAsync(IgnoredCallsignEntry entry, CancellationToken cancellationToken = default)
    {
        if (await _ignoredCallsignStore.RemoveAsync(entry, cancellationToken).ConfigureAwait(false))
        {
            await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _ignoredCallsignStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

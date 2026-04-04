using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class IgnoredCallsignViewModel
{
    private readonly ISettingsStore _settingsStore;
    private readonly Services.WatcherController _watcherController;

    public IgnoredCallsignViewModel(ISettingsStore settingsStore, Services.WatcherController watcherController)
    {
        _settingsStore = settingsStore;
        _watcherController = watcherController;
    }

    public async Task<IReadOnlyList<IgnoredCallsignEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return IgnoredCallsignMatcher.NormalizeEntries(settings.IgnoredCallsigns);
    }

    public async Task<bool> AddAsync(string callsign, string band, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var entries = IgnoredCallsignMatcher.NormalizeEntries(settings.IgnoredCallsigns);
        if (IgnoredCallsignMatcher.Contains(settings.IgnoredCallsignIndex, callsign, band))
        {
            return false;
        }

        entries.Add(new IgnoredCallsignEntry
        {
            Callsign = IgnoredCallsignMatcher.NormalizeCallsign(callsign),
            Band = IgnoredCallsignMatcher.NormalizeBand(band)
        });

        settings.IgnoredCallsigns = IgnoredCallsignMatcher.NormalizeEntries(entries).ToArray();
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IgnoredCallsignMergeResult> MergeAsync(
        IEnumerable<IgnoredCallsignEntry> importedEntries,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existingEntries = IgnoredCallsignMatcher.NormalizeEntries(settings.IgnoredCallsigns);
        var existingIndex = new HashSet<string>(settings.IgnoredCallsignIndex, StringComparer.Ordinal);
        var normalizedImportedEntries = IgnoredCallsignMatcher.NormalizeEntries(importedEntries);
        var addedCount = 0;

        foreach (var entry in normalizedImportedEntries)
        {
            var key = IgnoredCallsignMatcher.CreateLookupKey(entry.Callsign, entry.Band);
            if (string.IsNullOrWhiteSpace(key) || !existingIndex.Add(key))
            {
                continue;
            }

            existingEntries.Add(entry);
            addedCount++;
        }

        if (addedCount > 0)
        {
            settings.IgnoredCallsigns = IgnoredCallsignMatcher.NormalizeEntries(existingEntries).ToArray();
            await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
            await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        }

        return new IgnoredCallsignMergeResult
        {
            CandidateCount = normalizedImportedEntries.Count,
            AddedCount = addedCount,
            DuplicateCount = normalizedImportedEntries.Count - addedCount
        };
    }

    public async Task RemoveAsync(IgnoredCallsignEntry entry, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.IgnoredCallsigns = IgnoredCallsignMatcher
            .NormalizeEntries(settings.IgnoredCallsigns)
            .Where(existing =>
                !string.Equals(existing.Callsign, entry.Callsign, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.Band, entry.Band, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.IgnoredCallsigns = Array.Empty<IgnoredCallsignEntry>();
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.Services;

public sealed class RuleNamedSetResolver
{
    private readonly IIgnoredCallsignStore _ignoredCallsignStore;
    private readonly ISettingsStore _settingsStore;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private HashSet<string> _ignoredLookupKeys = new(StringComparer.Ordinal);
    private HashSet<string> _ignoredCallsigns = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<int> _selectedDxccIds = [];

    public RuleNamedSetResolver(IIgnoredCallsignStore ignoredCallsignStore, ISettingsStore settingsStore)
    {
        _ignoredCallsignStore = ignoredCallsignStore;
        _settingsStore = settingsStore;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var settingsTask = _settingsStore.LoadAsync(cancellationToken);
            var entries = await _ignoredCallsignStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var settings = await settingsTask.ConfigureAwait(false);
            _ignoredLookupKeys = new HashSet<string>(StringComparer.Ordinal);
            _ignoredCallsigns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _selectedDxccIds = [.. settings.PreferredDxccIds];
            foreach (var entry in entries)
            {
                var callsign = IgnoredCallsignMatcher.NormalizeCallsign(entry.Callsign);
                var band = IgnoredCallsignMatcher.NormalizeBand(entry.Band);
                if (string.IsNullOrWhiteSpace(callsign))
                {
                    continue;
                }

                _ignoredCallsigns.Add(callsign);
                if (!string.IsNullOrWhiteSpace(band))
                {
                    _ignoredLookupKeys.Add(IgnoredCallsignMatcher.CreateLookupKey(callsign, band));
                }
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public bool Contains(RuleNamedSetRef namedSetRef, string? fieldValue, RuleEvaluationContext context, NamedSetBandMatchMode bandMatchMode)
    {
        return namedSetRef switch
        {
            RuleNamedSetRef.IgnoredCallsigns => ContainsIgnoredCallsign(fieldValue, context, bandMatchMode),
            RuleNamedSetRef.DxccList => ContainsSelectedDxcc(fieldValue),
            _ => false
        };
    }

    private bool ContainsIgnoredCallsign(string? fieldValue, RuleEvaluationContext context, NamedSetBandMatchMode bandMatchMode)
    {
        var callsign = IgnoredCallsignMatcher.NormalizeCallsign(fieldValue);
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return false;
        }

        if (bandMatchMode == NamedSetBandMatchMode.IgnoreBand)
        {
            return _ignoredCallsigns.Contains(callsign);
        }

        var band = IgnoredCallsignMatcher.NormalizeBand(context.CurrentBand);
        return !string.IsNullOrWhiteSpace(band) && _ignoredLookupKeys.Contains(IgnoredCallsignMatcher.CreateLookupKey(callsign, band));
    }

    private bool ContainsSelectedDxcc(string? fieldValue)
    {
        return int.TryParse(fieldValue, out var dxccId) && _selectedDxccIds.Contains(dxccId);
    }
}

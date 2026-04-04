using System.Globalization;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Utilities;

public static class IgnoredCallsignMatcher
{
    public static IReadOnlySet<string> CreateIndex(IEnumerable<IgnoredCallsignEntry>? entries)
    {
        if (entries is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var index = entries as ICollection<IgnoredCallsignEntry> is { Count: > 0 } collection
            ? new HashSet<string>(collection.Count, StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            var key = CreateLookupKey(entry.Callsign, entry.Band);
            if (!string.IsNullOrWhiteSpace(key))
            {
                index.Add(key);
            }
        }

        return index;
    }

    public static List<IgnoredCallsignEntry> NormalizeEntries(IEnumerable<IgnoredCallsignEntry>? entries)
    {
        if (entries is null)
        {
            return [];
        }

        return entries
            .Select(entry => new IgnoredCallsignEntry
            {
                Callsign = NormalizeCallsign(entry.Callsign),
                Band = NormalizeBand(entry.Band)
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Callsign) && !string.IsNullOrWhiteSpace(entry.Band))
            .Distinct(IgnoredCallsignEntryComparer.Instance)
            .OrderBy(entry => entry.Band, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Callsign, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizeCallsign(string? value)
    {
        return WsjtxMessageParser.NormalizeCallsign(value);
    }

    public static string NormalizeBand(string? value)
    {
        var normalized = (value ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (normalized.EndsWith("mhz", StringComparison.Ordinal))
        {
            var mhzText = normalized[..^3];
            if (double.TryParse(mhzText, NumberStyles.Float, CultureInfo.InvariantCulture, out var mhz))
            {
                return RadioBandUtility.GetBandName(mhz * 1_000_000d);
            }
        }

        return normalized.All(character => char.IsDigit(character) || character == '.')
            ? normalized + "m"
            : normalized;
    }

    public static bool Contains(IEnumerable<IgnoredCallsignEntry> entries, string? callsign, string? band)
    {
        var normalizedCallsign = NormalizeCallsign(callsign);
        var normalizedBand = NormalizeBand(band);
        if (string.IsNullOrWhiteSpace(normalizedCallsign) || string.IsNullOrWhiteSpace(normalizedBand))
        {
            return false;
        }

        return entries.Any(entry =>
            string.Equals(entry.Callsign, normalizedCallsign, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(entry.Band, normalizedBand, StringComparison.OrdinalIgnoreCase));
    }

    public static bool Contains(IReadOnlySet<string> ignoredCallsignIndex, string? callsign, string? band)
    {
        var key = CreateLookupKey(callsign, band);
        return !string.IsNullOrWhiteSpace(key) && ignoredCallsignIndex.Contains(key);
    }

    public static bool IsIgnored(
        DecodedRadioMessage message,
        IEnumerable<IgnoredCallsignEntry> entries,
        IgnoredCallsignMatchTarget matchTarget)
    {
        var band = NormalizeBand(RadioBandUtility.GetBandName(message.DialFrequencyHz));
        if (string.IsNullOrWhiteSpace(band))
        {
            return false;
        }

        return matchTarget switch
        {
            IgnoredCallsignMatchTarget.Disabled => false,
            IgnoredCallsignMatchTarget.ReceiverOnly => Contains(entries, message.Receiver, band),
            IgnoredCallsignMatchTarget.ReceiverOrTransmitter => Contains(entries, message.Receiver, band) || Contains(entries, message.Transmitter, band),
            _ => Contains(entries, message.Transmitter, band)
        };
    }

    public static bool IsIgnored(DecodedRadioMessage message, AppSettings settings)
    {
        return IsIgnored(message, settings.IgnoredCallsignIndex, settings.IgnoredCallsignMatchTarget);
    }

    public static bool IsIgnored(
        DecodedRadioMessage message,
        IReadOnlySet<string> ignoredCallsignIndex,
        IgnoredCallsignMatchTarget matchTarget)
    {
        var band = NormalizeBand(RadioBandUtility.GetBandName(message.DialFrequencyHz));
        if (string.IsNullOrWhiteSpace(band))
        {
            return false;
        }

        return matchTarget switch
        {
            IgnoredCallsignMatchTarget.Disabled => false,
            IgnoredCallsignMatchTarget.ReceiverOnly => Contains(ignoredCallsignIndex, message.Receiver, band),
            IgnoredCallsignMatchTarget.ReceiverOrTransmitter => Contains(ignoredCallsignIndex, message.Receiver, band) || Contains(ignoredCallsignIndex, message.Transmitter, band),
            _ => Contains(ignoredCallsignIndex, message.Transmitter, band)
        };
    }

    public static string CreateLookupKey(string? callsign, string? band)
    {
        var normalizedCallsign = NormalizeCallsign(callsign);
        var normalizedBand = NormalizeBand(band);
        if (string.IsNullOrWhiteSpace(normalizedCallsign) || string.IsNullOrWhiteSpace(normalizedBand))
        {
            return string.Empty;
        }

        return string.Concat(normalizedBand, "|", normalizedCallsign);
    }

    private sealed class IgnoredCallsignEntryComparer : IEqualityComparer<IgnoredCallsignEntry>
    {
        public static IgnoredCallsignEntryComparer Instance { get; } = new();

        public bool Equals(IgnoredCallsignEntry? x, IgnoredCallsignEntry? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.Callsign, y.Callsign, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Band, y.Band, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(IgnoredCallsignEntry obj)
        {
            return HashCode.Combine(
                obj.Callsign.ToUpperInvariant(),
                obj.Band.ToUpperInvariant());
        }
    }
}

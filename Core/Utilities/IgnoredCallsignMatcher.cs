using System.Globalization;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Utilities;

public static class IgnoredCallsignMatcher
{
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
        return WsjtxMessageParser.NormalizeCallsign(value ?? string.Empty);
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

    public static bool Contains(IReadOnlySet<string> matchedLookupKeys, string? callsign, string? band)
    {
        var key = CreateLookupKey(callsign, band);
        return !string.IsNullOrWhiteSpace(key) && matchedLookupKeys.Contains(key);
    }

    public static void AddLookupKeys(
        ISet<string> lookupKeys,
        DecodedRadioMessage message,
        IgnoredCallsignMatchTarget matchTarget)
    {
        var band = NormalizeBand(RadioBandUtility.GetBandName(message.DialFrequencyHz));
        if (string.IsNullOrWhiteSpace(band) || matchTarget == IgnoredCallsignMatchTarget.Disabled)
        {
            return;
        }

        switch (matchTarget)
        {
            case IgnoredCallsignMatchTarget.ReceiverOnly:
                AddLookupKey(lookupKeys, message.Receiver, band);
                break;
            case IgnoredCallsignMatchTarget.ReceiverOrTransmitter:
                AddLookupKey(lookupKeys, message.Receiver, band);
                AddLookupKey(lookupKeys, message.Transmitter, band);
                break;
            default:
                AddLookupKey(lookupKeys, message.Transmitter, band);
                break;
        }
    }

    public static bool IsIgnored(
        DecodedRadioMessage message,
        IReadOnlySet<string> matchedLookupKeys,
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
            IgnoredCallsignMatchTarget.ReceiverOnly => Contains(matchedLookupKeys, message.Receiver, band),
            IgnoredCallsignMatchTarget.ReceiverOrTransmitter => Contains(matchedLookupKeys, message.Receiver, band) || Contains(matchedLookupKeys, message.Transmitter, band),
            _ => Contains(matchedLookupKeys, message.Transmitter, band)
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

    private static void AddLookupKey(ISet<string> lookupKeys, string? callsign, string band)
    {
        var key = CreateLookupKey(callsign, band);
        if (!string.IsNullOrWhiteSpace(key))
        {
            lookupKeys.Add(key);
        }
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

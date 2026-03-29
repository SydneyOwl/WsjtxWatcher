using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace WsjtxWatcher.Core.Utilities;

public static class CallsignPatternMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);
    private const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public static IReadOnlyList<string> NormalizePatterns(IEnumerable<string>? patterns)
    {
        return (patterns ?? [])
            .Select(pattern => (pattern ?? string.Empty).Trim())
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static bool IsMatch(string messageText, IEnumerable<string>? patterns)
    {
        var source = messageText ?? string.Empty;
        foreach (var pattern in NormalizePatterns(patterns))
        {
            if (TryCreateRegex(pattern, out var regex) && regex.IsMatch(source))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsValidPattern(string value)
    {
        return TryCreateRegex((value ?? string.Empty).Trim(), out _);
    }

    public static string CreateDefaultPattern(string callsign)
    {
        return Regex.Escape((callsign ?? string.Empty).Trim().ToUpperInvariant());
    }

    private static bool TryCreateRegex(string pattern, out Regex regex)
    {
        regex = null!;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            regex = RegexCache.GetOrAdd(pattern, key => new Regex(key, PatternOptions | RegexOptions.Compiled));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

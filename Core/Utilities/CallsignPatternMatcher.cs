using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace WsjtxWatcher.Core.Utilities;

public static class CallsignPatternMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);
    private const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly char[] TokenTrimCharacters = [',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '<', '>', '"', '\''];

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

    public static bool ContainsCallsign(string messageText, string callsign)
    {
        return FindCallsignTokenRanges(messageText, callsign).Count > 0;
    }

    public static IReadOnlyList<(int Start, int Length)> FindCallsignTokenRanges(string messageText, string callsign)
    {
        var source = messageText ?? string.Empty;
        var normalizedCallsign = NormalizeToken(callsign);
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(normalizedCallsign))
        {
            return [];
        }

        var matches = new List<(int Start, int Length)>();
        var index = 0;
        while (index < source.Length)
        {
            while (index < source.Length && char.IsWhiteSpace(source[index]))
            {
                index++;
            }

            var start = index;
            while (index < source.Length && !char.IsWhiteSpace(source[index]))
            {
                index++;
            }

            var length = index - start;
            if (length <= 0)
            {
                continue;
            }

            var token = source[start..index];
            if (TokenContainsCallsign(token, normalizedCallsign))
            {
                matches.Add((start, length));
            }
        }

        return matches;
    }

    public static string CreateDefaultPattern(string callsign)
    {
        return Regex.Escape((callsign ?? string.Empty).Trim().ToUpperInvariant());
    }

    private static bool TokenContainsCallsign(string token, string normalizedCallsign)
    {
        var normalizedToken = NormalizeToken(token);
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return false;
        }

        if (string.Equals(normalizedToken, normalizedCallsign, StringComparison.Ordinal))
        {
            return true;
        }

        return normalizedToken
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(segment => string.Equals(segment, normalizedCallsign, StringComparison.Ordinal));
    }

    private static string NormalizeToken(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Trim(TokenTrimCharacters)
            .ToUpperInvariant();
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

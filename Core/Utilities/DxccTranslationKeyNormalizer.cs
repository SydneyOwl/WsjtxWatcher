using System.Text.RegularExpressions;

namespace WsjtxWatcher.Core.Utilities;

public static partial class DxccTranslationKeyNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var result = InvalidDxccKeyCharacters().Replace(input, "_");
        result = ConsecutiveUnderscores().Replace(result, "_");
        result = result.Trim('_', ' ').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(result) ? string.Empty : $"dxcc_{result}";
    }

    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex InvalidDxccKeyCharacters();

    [GeneratedRegex("_+")]
    private static partial Regex ConsecutiveUnderscores();
}

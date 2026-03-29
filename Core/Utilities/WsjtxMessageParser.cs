namespace WsjtxWatcher.Core.Utilities;

public static class WsjtxMessageParser
{
    private static readonly string[] ControlTokens = ["CQ", "QRZ", "DE", "DX", "TEST", "POTA", "SOTA", "OTA"];
    
    public static string DecodeModeNotationsToString(string mode)
    {
        var normalizedMode = (mode ?? string.Empty).Trim().ToUpperInvariant();
        return normalizedMode switch
        {
            "`" => "FST4",
            "+" => "FT4",
            "~" => "FT8",
            "$" => "JT4",
            "@" => "JT9",
            "#" => "JT65",
            ":" => "Q65",
            "&" => "MSK144",
            _ => normalizedMode
        };
    }

    public static (string Transmitter, string Receiver) ParseParticipants(string messageText)
    {
        var tokens = SplitTokens(messageText);
        if (tokens.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (messageText.StartsWith("CQ", StringComparison.OrdinalIgnoreCase))
        {
            var transmitter = ExtractCqTransmitter(tokens);
            return string.IsNullOrWhiteSpace(transmitter)
                ? (string.Empty, string.Empty)
                : (NormalizeCallsign(transmitter), string.Empty);
        }

        var callsigns = tokens
            .Where(IsCallsignToken)
            .Select(NormalizeCallsign)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        return callsigns.Length switch
        {
            >= 2 => (callsigns[1], callsigns[0]),
            1 => (callsigns[0], string.Empty),
            _ => (string.Empty, string.Empty)
        };
    }

    public static GridUpdateCandidate? ExtractGridUpdate(string messageText)
    {
        var tokens = SplitTokens(messageText);
        if (tokens.Length < 2)
        {
            return null;
        }

        var grid = tokens[^1].Trim().ToUpperInvariant();
        if (!MaidenheadLocator.IsValid(grid))
        {
            return null;
        }

        var participants = ParseParticipants(messageText);
        if (string.IsNullOrWhiteSpace(participants.Transmitter) || !LooksLikeCallsign(participants.Transmitter))
        {
            return null;
        }

        return new GridUpdateCandidate(participants.Transmitter, grid);
    }

    public static string NormalizeCallsign(string value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string ExtractCqTransmitter(IReadOnlyList<string> tokens)
    {
        for (var index = tokens.Count - 1; index >= 1; index--)
        {
            if (MaidenheadLocator.IsValid(tokens[index]))
            {
                continue;
            }

            if (!IsCallsignToken(tokens[index]))
            {
                continue;
            }

            return tokens[index];
        }

        return string.Empty;
    }

    private static bool IsCallsignToken(string token)
    {
        var normalized = NormalizeCallsign(token);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (ControlTokens.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return !MaidenheadLocator.IsValid(normalized) && LooksLikeCallsign(normalized);
    }

    private static bool LooksLikeCallsign(string value)
    {
        return !value.Contains("...", StringComparison.Ordinal)
               && value.Any(char.IsLetter)
               && value.Any(char.IsDigit);
    }

    private static string[] SplitTokens(string messageText)
    {
        return (messageText ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

public readonly record struct GridUpdateCandidate(string Callsign, string GridSquare);

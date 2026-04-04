namespace WsjtxWatcher.Core.Models;

public sealed class DecodedRadioMessage
{
    public bool IsUserTransmit { get; init; }
    public bool IsSystemNotice { get; init; }
    public string DecodeTimeUtc { get; init; } = string.Empty;
    public int Snr { get; init; }
    public double OffsetTimeSeconds { get; init; }
    public int OffsetFrequencyHz { get; init; }
    public string Mode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool LowConfidence { get; init; }
    public bool OffAir { get; init; }
    public string Receiver { get; init; } = string.Empty;
    public string Transmitter { get; init; } = string.Empty;
    public string TransmitterGrid { get; init; } = string.Empty;
    public string DistanceText { get; init; } = string.Empty;
    public string ToCountryEnglish { get; init; } = string.Empty;
    public string ToCountryChinese { get; init; } = string.Empty;
    public string FromCountryEnglish { get; init; } = string.Empty;
    public string FromCountryChinese { get; init; } = string.Empty;
    public int ToCountryId { get; init; }
    public int FromCountryId { get; init; }
    public bool ContainsMyCallsign { get; init; }
    public bool MatchesWatchedCallsignPattern { get; init; }
    public bool MatchesSelectedDxcc { get; init; }
    public double DialFrequencyHz { get; init; }
    public bool IsIgnored { get; set; }

    public static DecodedRadioMessage CreateUserTransmit(string message, string mode)
    {
        return new DecodedRadioMessage
        {
            IsUserTransmit = true,
            Message = message,
            Mode = mode ?? string.Empty
        };
    }

    public static DecodedRadioMessage CreateSystemNotice(string message)
    {
        return new DecodedRadioMessage
        {
            IsSystemNotice = true,
            Message = message ?? string.Empty
        };
    }
}

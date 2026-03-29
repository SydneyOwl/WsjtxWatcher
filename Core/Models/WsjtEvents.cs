using System.Net;

namespace WsjtxWatcher.Core.Models;

public class WsjtSessionEvent
{
    public string ClientId { get; init; } = string.Empty;
    public EndPoint? SessionEndPoint { get; init; }
}

public sealed class WsjtDecodeEvent : WsjtSessionEvent
{
    public bool IsNew { get; init; }
    public long TimeMilliseconds { get; init; }
    public int Snr { get; init; }
    public double OffsetTimeSeconds { get; init; }
    public int OffsetFrequencyHz { get; init; }
    public string Mode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool LowConfidence { get; init; }
    public bool OffAir { get; init; }
    public string RemoteCallsign { get; init; } = string.Empty;
    public string RemoteGrid { get; init; } = string.Empty;
    public string DetailText { get; init; } = string.Empty;
    public double ReportedFrequencyHz { get; init; }
}

public sealed class WsjtStatusEvent : WsjtSessionEvent
{
    public string Mode { get; init; } = string.Empty;
    public string TxMode { get; init; } = string.Empty;
    public bool Transmitting { get; init; }
    public string TransmitMessage { get; init; } = string.Empty;
    public double DialFrequencyHz { get; init; }
}

namespace WsjtxWatcher.Core.Models;

public sealed class IgnoredCallsignEntry
{
    public string Callsign { get; init; } = string.Empty;
    public string Band { get; init; } = string.Empty;
}

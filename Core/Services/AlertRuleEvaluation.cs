using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Services;

public sealed class AlertRuleEvaluation
{
    public string RuleId { get; init; } = string.Empty;
    public AlertRuleKind Kind { get; init; }
    public bool SendNotification { get; init; }
    public bool Vibrate { get; init; }
    public int CooldownSeconds { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public string Band { get; init; } = string.Empty;
}

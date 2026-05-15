namespace WsjtxWatcher.Core.Models;

public sealed class RuleEvaluationContext
{
    public RuleTriggerType TriggerType { get; init; }
    public DecodedRadioMessage? Message { get; init; }
    public WsjtQsoLoggedEvent? LoggedQso { get; init; }
    public AppSettings Settings { get; init; } = new();
    public string CurrentBand { get; init; } = string.Empty;
    public Dictionary<RuleField, object?> CachedValues { get; init; } = new();
}

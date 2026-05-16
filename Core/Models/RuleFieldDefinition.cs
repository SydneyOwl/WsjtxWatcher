namespace WsjtxWatcher.Core.Models;

public sealed class RuleFieldDefinition
{
    public RuleField Field { get; init; }
    public RuleTriggerType TriggerType { get; init; }
    public RuleValueType ValueType { get; init; }
    public IReadOnlyList<RuleOperator> SupportedOperators { get; init; } = [];
}

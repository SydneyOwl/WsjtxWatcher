namespace WsjtxWatcher.Core.Models;

public sealed class RuleConstantPredicate : RuleConditionNode
{
    public bool Value { get; set; }

    public override RuleConstantPredicate Clone()
    {
        return new RuleConstantPredicate
        {
            Value = Value
        };
    }
}

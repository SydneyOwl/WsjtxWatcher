namespace WsjtxWatcher.Core.Models;

public sealed class RulePredicate : RuleConditionNode
{
    public RuleField Field { get; set; }
    public RuleOperator Operator { get; set; }
    public RuleOperand Operand { get; set; } = new();

    public override RulePredicate Clone()
    {
        return new RulePredicate
        {
            Field = Field,
            Operator = Operator,
            Operand = Operand.Clone()
        };
    }

    public static RulePredicate Create(RuleField field, RuleOperator @operator, RuleOperand operand)
    {
        return new RulePredicate
        {
            Field = field,
            Operator = @operator,
            Operand = operand
        };
    }
}

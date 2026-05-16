namespace WsjtxWatcher.Core.Models;

public sealed class RuleConditionGroup : RuleConditionNode
{
    public RuleConditionGroupMode Mode { get; set; } = RuleConditionGroupMode.All;
    public List<RuleConditionNode> Children { get; set; } = [];

    public override RuleConditionGroup Clone()
    {
        return new RuleConditionGroup
        {
            Mode = Mode,
            Children = [.. Children.Select(child => child.Clone())]
        };
    }

    public static RuleConditionGroup CreateAll(params RuleConditionNode[] children)
    {
        return new RuleConditionGroup
        {
            Mode = RuleConditionGroupMode.All,
            Children = [.. children]
        };
    }

    public static RuleConditionGroup CreateAny(params RuleConditionNode[] children)
    {
        return new RuleConditionGroup
        {
            Mode = RuleConditionGroupMode.Any,
            Children = [.. children]
        };
    }
}

using System.Text.Json.Serialization;

namespace WsjtxWatcher.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RuleConditionGroup), "group")]
[JsonDerivedType(typeof(RulePredicate), "predicate")]
[JsonDerivedType(typeof(RuleConstantPredicate), "constant")]
public abstract class RuleConditionNode
{
    public abstract RuleConditionNode Clone();
}

namespace WsjtxWatcher.Core.Models;

public enum RuleOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    Regex,
    In,
    NotIn,
    InNamedSet,
    NotInNamedSet,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    StartsWith,
    EndsWith,
    IsTrue,
    IsFalse,
    Exists,
    NotExists
}

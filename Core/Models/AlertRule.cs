using System.Text.Json.Serialization;

namespace WsjtxWatcher.Core.Models;

public sealed class AlertRule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RuleSource Source { get; set; } = RuleSource.UserDefined;
    public bool IsEnabled { get; set; }
    public RuleTriggerType TriggerType { get; set; } = RuleTriggerType.DecodeMessage;
    public RuleConditionGroup RootCondition { get; set; } = RuleConditionGroup.CreateAll();
    public RuleActionConfig Actions { get; set; } = new();
    public int CooldownSeconds { get; set; } = 10;
    public int Priority { get; set; } = 100;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool IsReadOnly => false;

    public AlertRule Clone()
    {
        return new AlertRule
        {
            Id = Id,
            Name = Name,
            Source = Source,
            IsEnabled = IsEnabled,
            TriggerType = TriggerType,
            RootCondition = RootCondition.Clone(),
            Actions = Actions.Clone(),
            CooldownSeconds = CooldownSeconds,
            Priority = Priority,
            SortOrder = SortOrder,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}

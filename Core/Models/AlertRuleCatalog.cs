namespace WsjtxWatcher.Core.Models;

public static class AlertRuleCatalog
{
    public const string SystemMyCallsignRuleId = "system_my_callsign";
    public const string SystemWatchedCallsignRuleId = "system_watched_callsign";
    public const string SystemAnyMessageRuleId = "system_any_message";
    public const string SystemDxccRuleId = "system_selected_dxcc";
    public const string SystemLoggedQsoRuleId = "system_logged_qso";
    public const int DefaultCooldownSeconds = 10;
    public const int DefaultPriority = 100;

    public static IReadOnlyList<AlertRule> CreateSystemRules()
    {
        return
        [
            CreateSystemRule(
                SystemMyCallsignRuleId,
                "My callsign",
                true,
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAny(RulePredicate.Create(RuleField.ReceiverCallsign, RuleOperator.Equals, RuleOperand.ForContextRef(RuleContextRef.MyCallsign)))),
            CreateSystemRule(
                SystemWatchedCallsignRuleId,
                "Watched callsign",
                true,
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAny()),
            CreateSystemRule(
                SystemAnyMessageRuleId,
                "Any message",
                false,
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAll(new RuleConstantPredicate { Value = true })),
            CreateSystemRule(
                SystemDxccRuleId,
                "Selected DXCC",
                true,
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAny(
                    RulePredicate.Create(RuleField.FromCountryId, RuleOperator.InNamedSet, RuleOperand.ForNamedSet(RuleNamedSetRef.DxccList)),
                    RulePredicate.Create(RuleField.ToCountryId, RuleOperator.InNamedSet, RuleOperand.ForNamedSet(RuleNamedSetRef.DxccList)))),
            CreateSystemRule(
                SystemLoggedQsoRuleId,
                "Logged QSO",
                true,
                RuleTriggerType.LoggedQso,
                RuleConditionGroup.CreateAll(new RuleConstantPredicate { Value = true }))
        ];
    }

    public static IReadOnlyCollection<int> DefaultSelectedDxccIds { get; } =
    [
        257,
        67,
        115,
        71,
        229,
        225,
        17,
        172,
        363,
        16
    ];

    public static List<AlertRule> NormalizeRules(IEnumerable<AlertRule>? rules)
    {
        return (rules ?? [])
            .Where(rule => rule is not null && !string.IsNullOrWhiteSpace(rule.Id))
            .Select(rule => SanitizeRule(rule.Clone()))
            .GroupBy(rule => rule.Id, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(rule => rule.Source == RuleSource.SystemPreset).First())
            .OrderBy(rule => rule.Source == RuleSource.SystemPreset ? 0 : 1)
            .ThenBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.CreatedAtUtc)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal)
            .ToList();
    }

    public static List<AlertRule> NormalizeCustomRules(IEnumerable<AlertRule>? rules)
    {
        return NormalizeRules(rules);
    }

    public static IReadOnlyList<AlertRule> GetAllRules(AppSettings settings)
    {
        return NormalizeRules(settings.AlertRules);
    }

    public static AlertRule GetRequiredRule(AppSettings settings, string ruleId)
    {
        var rule = FindRule(settings, ruleId);
        if (rule is null)
        {
            throw new InvalidOperationException($"Alert rule '{ruleId}' was not found.");
        }

        return rule;
    }

    public static AlertRule? FindRule(AppSettings settings, string ruleId)
    {
        return NormalizeRules(settings.AlertRules)
            .FirstOrDefault(rule => string.Equals(rule.Id, ruleId, StringComparison.Ordinal))?
            .Clone();
    }

    public static AlertRule CreateCustomRule(RuleTriggerType triggerType, int nextSortOrder)
    {
        return new AlertRule
        {
            Id = $"rule_{Guid.NewGuid():N}",
            Name = triggerType == RuleTriggerType.LoggedQso ? "New QSO rule" : "New message rule",
            Source = RuleSource.UserDefined,
            IsEnabled = true,
            TriggerType = triggerType,
            RootCondition = RuleConditionGroup.CreateAll(),
            Actions = new RuleActionConfig(),
            CooldownSeconds = DefaultCooldownSeconds,
            Priority = DefaultPriority,
            SortOrder = nextSortOrder,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void UpsertRule(AppSettings settings, AlertRule rule)
    {
        var normalized = NormalizeRules(settings.AlertRules);
        var sanitized = SanitizeRule(rule.Clone());
        var index = normalized.FindIndex(candidate => string.Equals(candidate.Id, sanitized.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            normalized[index] = sanitized;
        }
        else
        {
            normalized.Add(sanitized);
        }

        settings.AlertRules = NormalizeRules(normalized);
    }

    public static bool RemoveRule(AppSettings settings, string ruleId)
    {
        var normalized = NormalizeRules(settings.AlertRules);
        var removed = normalized.RemoveAll(rule => string.Equals(rule.Id, ruleId, StringComparison.Ordinal)) > 0;
        settings.AlertRules = NormalizeRules(normalized);
        return removed;
    }

    public static bool IsSystemRuleId(string ruleId)
    {
        return string.Equals(ruleId, SystemMyCallsignRuleId, StringComparison.Ordinal)
            || string.Equals(ruleId, SystemWatchedCallsignRuleId, StringComparison.Ordinal)
            || string.Equals(ruleId, SystemAnyMessageRuleId, StringComparison.Ordinal)
            || string.Equals(ruleId, SystemDxccRuleId, StringComparison.Ordinal)
            || string.Equals(ruleId, SystemLoggedQsoRuleId, StringComparison.Ordinal);
    }

    private static AlertRule CreateSystemRule(string id, string name, bool enableByDefault, RuleTriggerType triggerType, RuleConditionGroup rootCondition)
    {
        return new AlertRule
        {
            Id = id,
            Name = name,
            Source = RuleSource.SystemPreset,
            IsEnabled = enableByDefault,
            TriggerType = triggerType,
            RootCondition = rootCondition,
            Actions = new RuleActionConfig(),
            CooldownSeconds = DefaultCooldownSeconds,
            Priority = DefaultPriority,
            SortOrder = 0,
            CreatedAtUtc = DateTime.UnixEpoch
        };
    }

    private static AlertRule SanitizeRule(AlertRule rule)
    {
        rule.Id = (rule.Id ?? string.Empty).Trim();
        rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? "Unnamed rule" : rule.Name.Trim();
        rule.Source = rule.Source == RuleSource.SystemPreset ? RuleSource.SystemPreset : RuleSource.UserDefined;
        rule.CooldownSeconds = Math.Max(0, rule.CooldownSeconds);
        rule.Priority = Math.Max(0, rule.Priority);
        rule.RootCondition = rule.RootCondition?.Clone() ?? RuleConditionGroup.CreateAll();
        rule.Actions = rule.Actions?.Clone() ?? new RuleActionConfig();
        return rule;
    }
}

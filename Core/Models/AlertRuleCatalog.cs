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
                "我的呼号",
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAny(
                    RulePredicate.Create(RuleField.TransmitterCallsign, RuleOperator.Equals, RuleOperand.ForContextRef(RuleContextRef.MyCallsign)),
                    RulePredicate.Create(RuleField.ReceiverCallsign, RuleOperator.Equals, RuleOperand.ForContextRef(RuleContextRef.MyCallsign)))),
            CreateSystemRule(
                SystemWatchedCallsignRuleId,
                "指定呼号",
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAny(
                    RulePredicate.Create(RuleField.TransmitterCallsign, RuleOperator.Regex, RuleOperand.ForString("^JA")),
                    RulePredicate.Create(RuleField.ReceiverCallsign, RuleOperator.Regex, RuleOperand.ForString("^JA")))),
            CreateSystemRule(
                SystemAnyMessageRuleId,
                "任意消息",
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAll(new RuleConstantPredicate { Value = true })),
            CreateSystemRule(
                SystemDxccRuleId,
                "指定 DXCC",
                RuleTriggerType.DecodeMessage,
                RuleConditionGroup.CreateAny(
                    RulePredicate.Create(RuleField.FromCountryId, RuleOperator.In, RuleOperand.ForNumberList(DefaultSelectedDxccIds.Select(id => (double)id))),
                    RulePredicate.Create(RuleField.ToCountryId, RuleOperator.In, RuleOperand.ForNumberList(DefaultSelectedDxccIds.Select(id => (double)id))))),
            CreateSystemRule(
                SystemLoggedQsoRuleId,
                "QSO 完成",
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

    public static List<AlertRule> NormalizeCustomRules(IEnumerable<AlertRule>? rules)
    {
        return (rules ?? [])
            .Where(rule => rule is not null && !string.IsNullOrWhiteSpace(rule.Id))
            .Select(rule => SanitizeCustomRule(rule.Clone()))
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.CreatedAtUtc)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<AlertRule> GetAllRules(AppSettings settings)
    {
        return [.. CreateSystemRules(), .. NormalizeCustomRules(settings.AlertRules)];
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
        foreach (var systemRule in CreateSystemRules())
        {
            if (string.Equals(systemRule.Id, ruleId, StringComparison.Ordinal))
            {
                return systemRule.Clone();
            }
        }

        return NormalizeCustomRules(settings.AlertRules)
            .FirstOrDefault(rule => string.Equals(rule.Id, ruleId, StringComparison.Ordinal))?
            .Clone();
    }

    public static AlertRule CreateCustomRule(RuleTriggerType triggerType, int nextSortOrder)
    {
        return new AlertRule
        {
            Id = $"rule_{Guid.NewGuid():N}",
            Name = triggerType == RuleTriggerType.LoggedQso ? "新建 QSO 规则" : "新建消息规则",
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

    public static void UpsertCustomRule(AppSettings settings, AlertRule rule)
    {
        if (rule.Source == RuleSource.SystemPreset)
        {
            throw new InvalidOperationException("System preset rules are read-only.");
        }

        var normalized = NormalizeCustomRules(settings.AlertRules);
        var sanitized = SanitizeCustomRule(rule.Clone());
        var index = normalized.FindIndex(candidate => string.Equals(candidate.Id, sanitized.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            normalized[index] = sanitized;
        }
        else
        {
            normalized.Add(sanitized);
        }

        settings.AlertRules = NormalizeCustomRules(normalized);
    }

    public static bool RemoveCustomRule(AppSettings settings, string ruleId)
    {
        var normalized = NormalizeCustomRules(settings.AlertRules);
        var removed = normalized.RemoveAll(rule => string.Equals(rule.Id, ruleId, StringComparison.Ordinal)) > 0;
        settings.AlertRules = NormalizeCustomRules(normalized);
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

    private static AlertRule CreateSystemRule(string id, string name, RuleTriggerType triggerType, RuleConditionGroup rootCondition)
    {
        return new AlertRule
        {
            Id = id,
            Name = name,
            Source = RuleSource.SystemPreset,
            IsEnabled = true,
            TriggerType = triggerType,
            RootCondition = rootCondition,
            Actions = new RuleActionConfig(),
            CooldownSeconds = DefaultCooldownSeconds,
            Priority = DefaultPriority,
            SortOrder = 0,
            CreatedAtUtc = DateTime.UnixEpoch
        };
    }

    private static AlertRule SanitizeCustomRule(AlertRule rule)
    {
        rule.Id = (rule.Id ?? string.Empty).Trim();
        rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? "未命名规则" : rule.Name.Trim();
        rule.Source = RuleSource.UserDefined;
        rule.CooldownSeconds = Math.Max(0, rule.CooldownSeconds);
        rule.Priority = Math.Max(0, rule.Priority);
        rule.RootCondition = rule.RootCondition?.Clone() ?? RuleConditionGroup.CreateAll();
        rule.Actions = rule.Actions?.Clone() ?? new RuleActionConfig();
        return rule;
    }
}

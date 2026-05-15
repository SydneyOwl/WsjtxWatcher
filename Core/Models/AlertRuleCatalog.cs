using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.Models;

public static class AlertRuleCatalog
{
    public const string WatchedCallsignRuleId = "watched_callsign";
    public const string AnyMessageRuleId = "any_message";
    public const string SelectedDxccRuleId = "selected_dxcc";
    public const string LoggedQsoRuleId = "logged_qso";
    public const int DefaultCooldownSeconds = 10;

    public static IReadOnlyCollection<int> DefaultSelectedDxccIds { get; } = new[]
    {
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
    };

    public static List<AlertRule> CreateDefaultRules()
    {
        return
        [
            new AlertRule
            {
                Id = WatchedCallsignRuleId,
                Kind = AlertRuleKind.WatchedCallsign,
                CooldownSeconds = DefaultCooldownSeconds,
                MatchTarget = AlertRuleMatchTarget.TransmitterOnly
            },
            new AlertRule
            {
                Id = AnyMessageRuleId,
                Kind = AlertRuleKind.AnyMessage,
                CooldownSeconds = DefaultCooldownSeconds
            },
            new AlertRule
            {
                Id = SelectedDxccRuleId,
                Kind = AlertRuleKind.SelectedDxcc,
                CooldownSeconds = DefaultCooldownSeconds,
                MatchTarget = AlertRuleMatchTarget.TransmitterOnly,
                SelectedDxccIds = new HashSet<int>(DefaultSelectedDxccIds)
            },
            new AlertRule
            {
                Id = LoggedQsoRuleId,
                Kind = AlertRuleKind.LoggedQso,
                CooldownSeconds = DefaultCooldownSeconds
            }
        ];
    }

    public static List<AlertRule> Normalize(IEnumerable<AlertRule>? rules)
    {
        var normalized = (rules ?? [])
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Id))
            .GroupBy(rule => rule.Id, StringComparer.Ordinal)
            .Select(group => Sanitize(group.Last().Clone()))
            .ToDictionary(rule => rule.Id, StringComparer.Ordinal);

        foreach (var defaultRule in CreateDefaultRules())
        {
            if (!normalized.TryGetValue(defaultRule.Id, out var existing))
            {
                normalized[defaultRule.Id] = defaultRule;
                continue;
            }

            if (existing.Kind != defaultRule.Kind)
            {
                normalized[defaultRule.Id] = defaultRule;
            }
        }

        return normalized.Values
            .OrderBy(GetSortOrder)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal)
            .Select(rule => rule.Clone())
            .ToList();
    }

    public static AlertRule GetRequiredRule(AppSettings settings, string ruleId)
    {
        var rule = settings.AlertRules.FirstOrDefault(candidate => string.Equals(candidate.Id, ruleId, StringComparison.Ordinal));
        if (rule is null)
        {
            throw new InvalidOperationException($"Alert rule '{ruleId}' was not found.");
        }

        return rule;
    }

    public static AlertRule? FindRule(AppSettings settings, AlertRuleKind kind)
    {
        return settings.AlertRules.FirstOrDefault(rule => rule.Kind == kind);
    }

    public static void UpsertRule(AppSettings settings, AlertRule rule)
    {
        var index = settings.AlertRules.FindIndex(candidate => string.Equals(candidate.Id, rule.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            settings.AlertRules[index] = Sanitize(rule.Clone());
        }
        else
        {
            settings.AlertRules.Add(Sanitize(rule.Clone()));
        }

        settings.AlertRules = Normalize(settings.AlertRules);
    }

    public static IReadOnlyList<string> GetEffectiveCallsignPatterns(AlertRule rule, string myCallsign)
    {
        var normalizedPatterns = CallsignPatternMatcher.NormalizePatterns(rule.CallsignPatterns);
        if (normalizedPatterns.Count > 0)
        {
            return normalizedPatterns;
        }

        var defaultPattern = CallsignPatternMatcher.CreateDefaultPattern(myCallsign);
        return string.IsNullOrWhiteSpace(defaultPattern)
            ? []
            : [defaultPattern];
    }

    public static AlertRule Sanitize(AlertRule rule)
    {
        rule.Id = (rule.Id ?? string.Empty).Trim();
        rule.CooldownSeconds = Math.Max(0, rule.CooldownSeconds);
        rule.CallsignPatterns = [.. CallsignPatternMatcher.NormalizePatterns(rule.CallsignPatterns)];
        rule.SelectedDxccIds = new HashSet<int>(rule.SelectedDxccIds.Where(id => id > 0));
        return rule;
    }

    private static int GetSortOrder(AlertRule rule)
    {
        return rule.Kind switch
        {
            AlertRuleKind.WatchedCallsign => 0,
            AlertRuleKind.AnyMessage => 1,
            AlertRuleKind.SelectedDxcc => 2,
            AlertRuleKind.LoggedQso => 3,
            _ => 100
        };
    }
}

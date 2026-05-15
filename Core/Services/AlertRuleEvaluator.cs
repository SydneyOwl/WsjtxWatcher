using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Services;

public sealed class AlertRuleEvaluator
{
    public IReadOnlyList<AlertRuleEvaluation> Evaluate(DecodedRadioMessage message, AppSettings settings)
    {
        var results = new List<AlertRuleEvaluation>();
        foreach (var rule in settings.AlertRules)
        {
            if (!AlertRuleMatcher.Matches(message, rule, settings) || (!rule.SendNotification && !rule.Vibrate))
            {
                continue;
            }

            results.Add(new AlertRuleEvaluation
            {
                RuleId = rule.Id,
                Kind = rule.Kind,
                SendNotification = rule.SendNotification,
                Vibrate = rule.Vibrate,
                CooldownSeconds = rule.CooldownSeconds,
                Message = message.Message
            });
        }

        return results;
    }

    public AlertRuleEvaluation? Evaluate(WsjtQsoLoggedEvent qsoLoggedEvent, AppSettings settings, string callsign, string band)
    {
        var rule = settings.AlertRules.FirstOrDefault(candidate => candidate.Kind == AlertRuleKind.LoggedQso && candidate.IsEnabled);
        if (rule is null || !AlertRuleMatcher.MatchesLoggedQso(qsoLoggedEvent, rule) || (!rule.SendNotification && !rule.Vibrate))
        {
            return null;
        }

        return new AlertRuleEvaluation
        {
            RuleId = rule.Id,
            Kind = rule.Kind,
            SendNotification = rule.SendNotification,
            Vibrate = rule.Vibrate,
            CooldownSeconds = rule.CooldownSeconds,
            Callsign = callsign,
            Band = band
        };
    }
}

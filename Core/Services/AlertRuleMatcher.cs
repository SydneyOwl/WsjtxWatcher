using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.Services;

public static class AlertRuleMatcher
{
    public static bool Matches(DecodedRadioMessage message, AlertRule rule, AppSettings settings)
    {
        if (!rule.IsEnabled)
        {
            return false;
        }

        return rule.Kind switch
        {
            AlertRuleKind.WatchedCallsign => MatchesWatchedCallsign(message, rule, settings.MyCallsign),
            AlertRuleKind.AnyMessage => true,
            AlertRuleKind.SelectedDxcc => MatchesSelectedDxcc(message, rule),
            _ => false
        };
    }

    public static bool MatchesLoggedQso(WsjtQsoLoggedEvent loggedQso, AlertRule rule)
    {
        _ = loggedQso;
        return rule.IsEnabled && rule.Kind == AlertRuleKind.LoggedQso;
    }

    public static bool MatchesWatchedCallsign(DecodedRadioMessage message, AlertRule rule, string myCallsign)
    {
        var patterns = AlertRuleCatalog.GetEffectiveCallsignPatterns(rule, myCallsign);
        if (patterns.Count == 0)
        {
            return false;
        }

        var transmitterMatch = MatchesCallsignPattern(message.Transmitter, patterns);
        var receiverMatch = MatchesCallsignPattern(message.Receiver, patterns);
        return rule.MatchTarget switch
        {
            AlertRuleMatchTarget.ReceiverOnly => receiverMatch,
            AlertRuleMatchTarget.ReceiverOrTransmitter => receiverMatch || transmitterMatch,
            _ => transmitterMatch
        };
    }

    public static bool MatchesSelectedDxcc(DecodedRadioMessage message, AlertRule rule)
    {
        if (rule.SelectedDxccIds.Count == 0)
        {
            return false;
        }

        var transmitterMatch = message.FromCountryId > 0 && rule.SelectedDxccIds.Contains(message.FromCountryId);
        var receiverMatch = message.ToCountryId > 0 && rule.SelectedDxccIds.Contains(message.ToCountryId);
        return rule.MatchTarget switch
        {
            AlertRuleMatchTarget.ReceiverOnly => receiverMatch,
            AlertRuleMatchTarget.ReceiverOrTransmitter => receiverMatch || transmitterMatch,
            _ => transmitterMatch
        };
    }

    public static bool MatchesAnyWatchedCallsignRule(AppSettings settings, DecodedRadioMessage message)
    {
        return settings.AlertRules.Any(rule => rule.Kind == AlertRuleKind.WatchedCallsign && rule.IsEnabled && MatchesWatchedCallsign(message, rule, settings.MyCallsign));
    }

    public static bool MatchesAnySelectedDxccRule(AppSettings settings, DecodedRadioMessage message)
    {
        return settings.AlertRules.Any(rule => rule.Kind == AlertRuleKind.SelectedDxcc && rule.IsEnabled && MatchesSelectedDxcc(message, rule));
    }

    private static bool MatchesCallsignPattern(string callsign, IEnumerable<string> patterns)
    {
        return !string.IsNullOrWhiteSpace(callsign) && CallsignPatternMatcher.IsMatch(callsign, patterns);
    }
}

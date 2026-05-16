using Android.Content;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.UI;

internal static class RuleTextFormatter
{
    public static string GetRuleDisplayName(Context context, AlertRule rule)
    {
        if (rule.Source == RuleSource.SystemPreset)
        {
            return rule.Id switch
            {
                AlertRuleCatalog.SystemMyCallsignRuleId => context.GetString(Resource.String.when_my_callsign_included),
                AlertRuleCatalog.SystemWatchedCallsignRuleId => context.GetString(Resource.String.when_callsign_included),
                AlertRuleCatalog.SystemAnyMessageRuleId => context.GetString(Resource.String.when_call_all),
                AlertRuleCatalog.SystemDxccRuleId => context.GetString(Resource.String.on_dxcc),
                AlertRuleCatalog.SystemLoggedQsoRuleId => context.GetString(Resource.String.on_logged_qso),
                _ => rule.Name
            };
        }

        return string.IsNullOrWhiteSpace(rule.Name) ? context.GetString(Resource.String.unnamed_rule) : rule.Name;
    }

    public static string GetRuleSourceLabel(Context context, RuleSource source)
    {
        return source == RuleSource.SystemPreset
            ? context.GetString(Resource.String.rule_source_system)
            : context.GetString(Resource.String.rule_source_custom);
    }

    public static string GetTriggerLabel(Context context, RuleTriggerType triggerType)
    {
        return triggerType == RuleTriggerType.LoggedQso
            ? context.GetString(Resource.String.trigger_logged_qso)
            : context.GetString(Resource.String.trigger_decode_message);
    }

    public static string GetGroupModeLabel(Context context, RuleConditionGroupMode mode)
    {
        return mode == RuleConditionGroupMode.Any
            ? context.GetString(Resource.String.rule_group_any)
            : context.GetString(Resource.String.rule_group_all);
    }

    public static string GetFieldLabel(Context context, RuleField field)
    {
        return field switch
        {
            RuleField.MessageText => context.GetString(Resource.String.rule_field_message_text),
            RuleField.TransmitterCallsign => context.GetString(Resource.String.rule_field_transmitter_callsign),
            RuleField.ReceiverCallsign => context.GetString(Resource.String.rule_field_receiver_callsign),
            RuleField.Mode => context.GetString(Resource.String.rule_field_mode),
            RuleField.Snr => context.GetString(Resource.String.rule_field_snr),
            RuleField.OffsetFrequencyHz => context.GetString(Resource.String.rule_field_offset_frequency_hz),
            RuleField.OffsetTimeSeconds => context.GetString(Resource.String.rule_field_offset_time_seconds),
            RuleField.DialFrequencyHz => context.GetString(Resource.String.rule_field_dial_frequency_hz),
            RuleField.CurrentBand => context.GetString(Resource.String.rule_field_current_band),
            RuleField.TransmitterGrid => context.GetString(Resource.String.rule_field_transmitter_grid),
            RuleField.FromCountryId => context.GetString(Resource.String.rule_field_from_country_id),
            RuleField.ToCountryId => context.GetString(Resource.String.rule_field_to_country_id),
            RuleField.IsLowConfidence => context.GetString(Resource.String.rule_field_is_low_confidence),
            RuleField.IsOffAir => context.GetString(Resource.String.rule_field_is_off_air),
            RuleField.IsUserTransmit => context.GetString(Resource.String.rule_field_is_user_transmit),
            RuleField.IsSystemNotice => context.GetString(Resource.String.rule_field_is_system_notice),
            RuleField.LoggedQsoCallsign => context.GetString(Resource.String.rule_field_logged_qso_callsign),
            RuleField.LoggedQsoBand => context.GetString(Resource.String.rule_field_logged_qso_band),
            _ => field.ToString()
        };
    }

    public static string GetOperatorLabel(Context context, RuleOperator @operator)
    {
        return @operator switch
        {
            RuleOperator.Equals => context.GetString(Resource.String.rule_operator_equals),
            RuleOperator.NotEquals => context.GetString(Resource.String.rule_operator_not_equals),
            RuleOperator.Contains => context.GetString(Resource.String.rule_operator_contains),
            RuleOperator.NotContains => context.GetString(Resource.String.rule_operator_not_contains),
            RuleOperator.Regex => context.GetString(Resource.String.rule_operator_regex),
            RuleOperator.In => context.GetString(Resource.String.rule_operator_in),
            RuleOperator.NotIn => context.GetString(Resource.String.rule_operator_not_in),
            RuleOperator.InNamedSet => context.GetString(Resource.String.rule_operator_in_named_set),
            RuleOperator.NotInNamedSet => context.GetString(Resource.String.rule_operator_not_in_named_set),
            RuleOperator.GreaterThan => context.GetString(Resource.String.rule_operator_greater_than),
            RuleOperator.GreaterThanOrEqual => context.GetString(Resource.String.rule_operator_greater_than_or_equal),
            RuleOperator.LessThan => context.GetString(Resource.String.rule_operator_less_than),
            RuleOperator.LessThanOrEqual => context.GetString(Resource.String.rule_operator_less_than_or_equal),
            RuleOperator.StartsWith => context.GetString(Resource.String.rule_operator_starts_with),
            RuleOperator.EndsWith => context.GetString(Resource.String.rule_operator_ends_with),
            RuleOperator.IsTrue => context.GetString(Resource.String.rule_operator_is_true),
            RuleOperator.IsFalse => context.GetString(Resource.String.rule_operator_is_false),
            RuleOperator.Exists => context.GetString(Resource.String.rule_operator_exists),
            RuleOperator.NotExists => context.GetString(Resource.String.rule_operator_not_exists),
            _ => @operator.ToString()
        };
    }

    public static string GetNamedSetLabel(Context context, RuleNamedSetRef namedSetRef)
    {
        return namedSetRef switch
        {
            RuleNamedSetRef.IgnoredCallsigns => context.GetString(Resource.String.rule_named_set_ignored_callsigns),
            _ => namedSetRef.ToString()
        };
    }

    public static string GetBandMatchModeLabel(Context context, NamedSetBandMatchMode bandMatchMode)
    {
        return bandMatchMode == NamedSetBandMatchMode.IgnoreBand
            ? context.GetString(Resource.String.named_set_ignore_band)
            : context.GetString(Resource.String.named_set_match_band);
    }

    public static string BuildRuleSummary(Context context, AlertRule rule)
    {
        var action = rule.Actions.SendNotification && rule.Actions.Vibrate
            ? context.GetString(Resource.String.alert_rule_actions_notify_and_vibration)
            : rule.Actions.SendNotification
                ? context.GetString(Resource.String.alert_rule_actions_notify_only)
                : rule.Actions.Vibrate
                    ? context.GetString(Resource.String.alert_rule_actions_vibration_only)
                    : context.GetString(Resource.String.alert_rule_actions_none);

        var condition = BuildNodeSummary(context, rule.RootCondition);
        return string.Join(" · ", new[]
        {
            GetTriggerLabel(context, rule.TriggerType),
            $"P{rule.Priority}",
            action,
            string.Format(context.GetString(Resource.String.alert_rule_summary_cooldown), rule.CooldownSeconds),
            condition
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public static string BuildNodeSummary(Context context, RuleConditionNode node)
    {
        return node switch
        {
            RuleConditionGroup group => BuildGroupSummary(context, group),
            RulePredicate predicate => BuildPredicateSummary(context, predicate),
            RuleConstantPredicate constantPredicate => constantPredicate.Value
                ? context.GetString(Resource.String.rule_constant_true)
                : context.GetString(Resource.String.rule_constant_false),
            _ => string.Empty
        };
    }

    public static string BuildGroupSummary(Context context, RuleConditionGroup group)
    {
        if (group.Children.Count == 0)
        {
            return context.GetString(Resource.String.rule_group_empty);
        }

        var separator = group.Mode == RuleConditionGroupMode.Any ? " OR " : " AND ";
        return $"({string.Join(separator, group.Children.Select(child => BuildNodeSummary(context, child)))})";
    }

    public static string BuildPredicateSummary(Context context, RulePredicate predicate)
    {
        return $"{GetFieldLabel(context, predicate.Field)} {GetOperatorLabel(context, predicate.Operator)} {FormatOperand(context, predicate.Operand)}";
    }

    public static string FormatOperand(Context context, RuleOperand operand)
    {
        return operand.Kind switch
        {
            RuleValueType.String => operand.StringValue ?? string.Empty,
            RuleValueType.Number => operand.NumberValue?.ToString() ?? string.Empty,
            RuleValueType.Boolean => operand.BooleanValue?.ToString() ?? string.Empty,
            RuleValueType.StringList => string.Join(", ", operand.StringListValue),
            RuleValueType.NumberList => string.Join(", ", operand.NumberListValue),
            RuleValueType.ContextRef => operand.ContextRefValue switch
            {
                RuleContextRef.MyCallsign => $"${context.GetString(Resource.String.context_my_callsign)}",
                RuleContextRef.MyGrid => $"${context.GetString(Resource.String.context_my_grid)}",
                _ => string.Empty
            },
            RuleValueType.NamedSetRef when operand.NamedSetRefValue.HasValue =>
                $"{GetNamedSetLabel(context, operand.NamedSetRefValue.Value)} [{GetBandMatchModeLabel(context, operand.NamedSetBandMatchModeValue)}]",
            _ => string.Empty
        };
    }
}

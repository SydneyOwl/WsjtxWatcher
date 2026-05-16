using System.Text.RegularExpressions;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Services;

public sealed class AlertRuleEvaluator
{
    private readonly RuleFieldValueResolver _fieldValueResolver;
    private readonly RuleNamedSetResolver _namedSetResolver;

    public AlertRuleEvaluator(RuleFieldValueResolver fieldValueResolver, RuleNamedSetResolver namedSetResolver)
    {
        _fieldValueResolver = fieldValueResolver;
        _namedSetResolver = namedSetResolver;
    }

    public IReadOnlyList<AlertRuleEvaluation> Evaluate(DecodedRadioMessage message, AppSettings settings)
    {
        var context = new RuleEvaluationContext
        {
            TriggerType = RuleTriggerType.DecodeMessage,
            Message = message,
            Settings = settings,
            CurrentBand = message.CurrentBand
        };

        return Evaluate(settings.AlertRules, context, message.Message, string.Empty, string.Empty);
    }

    public IReadOnlyList<AlertRuleEvaluation> Evaluate(WsjtQsoLoggedEvent qsoLoggedEvent, AppSettings settings, string callsign, string band)
    {
        var context = new RuleEvaluationContext
        {
            TriggerType = RuleTriggerType.LoggedQso,
            LoggedQso = qsoLoggedEvent,
            Settings = settings,
            CurrentBand = band
        };

        return Evaluate(settings.AlertRules, context, string.Empty, callsign, band);
    }

    private IReadOnlyList<AlertRuleEvaluation> Evaluate(
        IEnumerable<AlertRule> rules,
        RuleEvaluationContext context,
        string message,
        string callsign,
        string band)
    {
        return rules
            .Where(rule => rule.IsEnabled)
            .Where(rule => rule.TriggerType == context.TriggerType)
            .Where(rule => rule.Actions.SendNotification || rule.Actions.Vibrate)
            .Where(rule => EvaluateNode(rule.RootCondition, context))
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAtUtc)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal)
            .Select(rule => new AlertRuleEvaluation
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                SendNotification = rule.Actions.SendNotification,
                Vibrate = rule.Actions.Vibrate,
                CooldownSeconds = rule.CooldownSeconds,
                Priority = rule.Priority,
                CreatedAtUtc = rule.CreatedAtUtc,
                Message = message,
                Callsign = callsign,
                Band = band
            })
            .ToList();
    }

    private bool EvaluateNode(RuleConditionNode node, RuleEvaluationContext context)
    {
        return node switch
        {
            RuleConditionGroup group => EvaluateGroup(group, context),
            RulePredicate predicate => EvaluatePredicate(predicate, context),
            RuleConstantPredicate constantPredicate => constantPredicate.Value,
            _ => false
        };
    }

    private bool EvaluateGroup(RuleConditionGroup group, RuleEvaluationContext context)
    {
        if (group.Children.Count == 0)
        {
            return false;
        }

        return group.Mode == RuleConditionGroupMode.All
            ? group.Children.All(child => EvaluateNode(child, context))
            : group.Children.Any(child => EvaluateNode(child, context));
    }

    private bool EvaluatePredicate(RulePredicate predicate, RuleEvaluationContext context)
    {
        var fieldValue = _fieldValueResolver.Resolve(predicate.Field, context);
        var operandValue = ResolveOperandValue(predicate.Operand, context);
        return ApplyOperator(predicate.Operator, fieldValue, operandValue, predicate.Operand, context);
    }

    private object? ResolveOperandValue(RuleOperand operand, RuleEvaluationContext context)
    {
        return operand.Kind switch
        {
            RuleValueType.String => operand.StringValue,
            RuleValueType.Number => operand.NumberValue,
            RuleValueType.Boolean => operand.BooleanValue,
            RuleValueType.StringList => operand.StringListValue,
            RuleValueType.NumberList => operand.NumberListValue,
            RuleValueType.ContextRef when operand.ContextRefValue.HasValue => _fieldValueResolver.ResolveContextRef(operand.ContextRefValue.Value, context),
            RuleValueType.NamedSetRef => operand.NamedSetRefValue,
            _ => null
        };
    }

    private bool ApplyOperator(RuleOperator @operator, object? fieldValue, object? operandValue, RuleOperand operand, RuleEvaluationContext context)
    {
        return @operator switch
        {
            RuleOperator.Equals => Compare(fieldValue, operandValue) == 0,
            RuleOperator.NotEquals => Compare(fieldValue, operandValue) != 0,
            RuleOperator.Contains => Contains(fieldValue, operandValue),
            RuleOperator.NotContains => !Contains(fieldValue, operandValue),
            RuleOperator.Regex => RegexMatch(fieldValue, operandValue),
            RuleOperator.In => In(fieldValue, operandValue),
            RuleOperator.NotIn => !In(fieldValue, operandValue),
            RuleOperator.InNamedSet => InNamedSet(fieldValue, operand, context),
            RuleOperator.NotInNamedSet => !InNamedSet(fieldValue, operand, context),
            RuleOperator.GreaterThan => Compare(fieldValue, operandValue) > 0,
            RuleOperator.GreaterThanOrEqual => Compare(fieldValue, operandValue) >= 0,
            RuleOperator.LessThan => Compare(fieldValue, operandValue) < 0,
            RuleOperator.LessThanOrEqual => Compare(fieldValue, operandValue) <= 0,
            RuleOperator.StartsWith => StartsWith(fieldValue, operandValue),
            RuleOperator.EndsWith => EndsWith(fieldValue, operandValue),
            RuleOperator.IsTrue => fieldValue is bool boolValue && boolValue,
            RuleOperator.IsFalse => fieldValue is bool boolValue && !boolValue,
            RuleOperator.Exists => Exists(fieldValue),
            RuleOperator.NotExists => !Exists(fieldValue),
            _ => false
        };
    }

    private bool InNamedSet(object? fieldValue, RuleOperand operand, RuleEvaluationContext context)
    {
        if (operand.NamedSetRefValue is null)
        {
            return false;
        }

        return _namedSetResolver.Contains(
            operand.NamedSetRefValue.Value,
            fieldValue?.ToString(),
            context,
            operand.NamedSetBandMatchModeValue);
    }

    private static int Compare(object? left, object? right)
    {
        if (TryGetNumber(left, out var leftNumber) && TryGetNumber(right, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        var leftText = NormalizeString(left);
        var rightText = NormalizeString(right);
        return string.Compare(leftText, rightText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(object? left, object? right)
    {
        var leftText = NormalizeString(left);
        var rightText = NormalizeString(right);
        return !string.IsNullOrWhiteSpace(leftText) && !string.IsNullOrWhiteSpace(rightText)
            && leftText.Contains(rightText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RegexMatch(object? left, object? right)
    {
        var source = NormalizeString(left);
        var pattern = NormalizeString(right);
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool In(object? left, object? right)
    {
        if (right is IEnumerable<string> stringValues)
        {
            var source = NormalizeString(left);
            return stringValues.Any(value => string.Equals(source, NormalizeString(value), StringComparison.OrdinalIgnoreCase));
        }

        if (right is IEnumerable<double> numberValues && TryGetNumber(left, out var number))
        {
            return numberValues.Any(value => Math.Abs(value - number) < 0.0001d);
        }

        return false;
    }

    private static bool StartsWith(object? left, object? right)
    {
        var source = NormalizeString(left);
        var prefix = NormalizeString(right);
        return !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(prefix)
            && source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndsWith(object? left, object? right)
    {
        var source = NormalizeString(left);
        var suffix = NormalizeString(right);
        return !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(suffix)
            && source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Exists(object? value)
    {
        return value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            IEnumerable<string> stringValues => stringValues.Any(),
            IEnumerable<double> numberValues => numberValues.Any(),
            _ => true
        };
    }

    private static string NormalizeString(object? value)
    {
        return (value?.ToString() ?? string.Empty).Trim();
    }

    private static bool TryGetNumber(object? value, out double number)
    {
        switch (value)
        {
            case null:
                number = 0d;
                return false;
            case int intValue:
                number = intValue;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case float floatValue:
                number = floatValue;
                return true;
            case double doubleValue:
                number = doubleValue;
                return true;
            case decimal decimalValue:
                number = (double)decimalValue;
                return true;
            default:
                return double.TryParse(value.ToString(), out number);
        }
    }
}

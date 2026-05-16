namespace WsjtxWatcher.Core.Models;

public static class RuleMetadataCatalog
{
    private static readonly IReadOnlyList<RuleFieldDefinition> Definitions =
    [
        Define(RuleField.MessageText, RuleTriggerType.DecodeMessage, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.NotContains, RuleOperator.Regex, RuleOperator.StartsWith, RuleOperator.EndsWith, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.TransmitterCallsign, RuleTriggerType.DecodeMessage, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.StartsWith, RuleOperator.EndsWith, RuleOperator.In, RuleOperator.NotIn, RuleOperator.InNamedSet, RuleOperator.NotInNamedSet, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.ReceiverCallsign, RuleTriggerType.DecodeMessage, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.StartsWith, RuleOperator.EndsWith, RuleOperator.In, RuleOperator.NotIn, RuleOperator.InNamedSet, RuleOperator.NotInNamedSet, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.Mode, RuleTriggerType.DecodeMessage, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.In, RuleOperator.NotIn, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.Snr, RuleTriggerType.DecodeMessage, RuleValueType.Number, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.GreaterThan, RuleOperator.GreaterThanOrEqual, RuleOperator.LessThan, RuleOperator.LessThanOrEqual),
        Define(RuleField.OffsetFrequencyHz, RuleTriggerType.DecodeMessage, RuleValueType.Number, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.GreaterThan, RuleOperator.GreaterThanOrEqual, RuleOperator.LessThan, RuleOperator.LessThanOrEqual),
        Define(RuleField.OffsetTimeSeconds, RuleTriggerType.DecodeMessage, RuleValueType.Number, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.GreaterThan, RuleOperator.GreaterThanOrEqual, RuleOperator.LessThan, RuleOperator.LessThanOrEqual),
        Define(RuleField.DialFrequencyHz, RuleTriggerType.DecodeMessage, RuleValueType.Number, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.GreaterThan, RuleOperator.GreaterThanOrEqual, RuleOperator.LessThan, RuleOperator.LessThanOrEqual),
        Define(RuleField.CurrentBand, RuleTriggerType.DecodeMessage, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.In, RuleOperator.NotIn, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.TransmitterGrid, RuleTriggerType.DecodeMessage, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.FromCountryId, RuleTriggerType.DecodeMessage, RuleValueType.Number, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.In, RuleOperator.NotIn),
        Define(RuleField.ToCountryId, RuleTriggerType.DecodeMessage, RuleValueType.Number, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.In, RuleOperator.NotIn),
        Define(RuleField.IsLowConfidence, RuleTriggerType.DecodeMessage, RuleValueType.Boolean, RuleOperator.IsTrue, RuleOperator.IsFalse, RuleOperator.Equals, RuleOperator.NotEquals),
        Define(RuleField.IsOffAir, RuleTriggerType.DecodeMessage, RuleValueType.Boolean, RuleOperator.IsTrue, RuleOperator.IsFalse, RuleOperator.Equals, RuleOperator.NotEquals),
        Define(RuleField.IsUserTransmit, RuleTriggerType.DecodeMessage, RuleValueType.Boolean, RuleOperator.IsTrue, RuleOperator.IsFalse, RuleOperator.Equals, RuleOperator.NotEquals),
        Define(RuleField.IsSystemNotice, RuleTriggerType.DecodeMessage, RuleValueType.Boolean, RuleOperator.IsTrue, RuleOperator.IsFalse, RuleOperator.Equals, RuleOperator.NotEquals),
        Define(RuleField.LoggedQsoCallsign, RuleTriggerType.LoggedQso, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.StartsWith, RuleOperator.EndsWith, RuleOperator.In, RuleOperator.NotIn, RuleOperator.InNamedSet, RuleOperator.NotInNamedSet, RuleOperator.Exists, RuleOperator.NotExists),
        Define(RuleField.LoggedQsoBand, RuleTriggerType.LoggedQso, RuleValueType.String, RuleOperator.Equals, RuleOperator.NotEquals, RuleOperator.Contains, RuleOperator.Regex, RuleOperator.In, RuleOperator.NotIn, RuleOperator.Exists, RuleOperator.NotExists)
    ];

    public static IReadOnlyList<RuleFieldDefinition> GetFieldDefinitions(RuleTriggerType triggerType)
    {
        return Definitions.Where(definition => definition.TriggerType == triggerType).ToList();
    }

    public static RuleFieldDefinition GetRequiredFieldDefinition(RuleField field, RuleTriggerType triggerType)
    {
        return Definitions.First(definition => definition.Field == field && definition.TriggerType == triggerType);
    }

    private static RuleFieldDefinition Define(RuleField field, RuleTriggerType triggerType, RuleValueType valueType, params RuleOperator[] supportedOperators)
    {
        return new RuleFieldDefinition
        {
            Field = field,
            TriggerType = triggerType,
            ValueType = valueType,
            SupportedOperators = supportedOperators
        };
    }
}

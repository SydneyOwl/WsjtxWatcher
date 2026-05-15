namespace WsjtxWatcher.Core.Models;

public sealed class RuleOperand
{
    public RuleValueType Kind { get; set; }
    public string? StringValue { get; set; }
    public double? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public List<string> StringListValue { get; set; } = [];
    public List<double> NumberListValue { get; set; } = [];
    public RuleContextRef? ContextRefValue { get; set; }
    public RuleNamedSetRef? NamedSetRefValue { get; set; }
    public NamedSetBandMatchMode NamedSetBandMatchModeValue { get; set; } = NamedSetBandMatchMode.MatchBand;

    public RuleOperand Clone()
    {
        return new RuleOperand
        {
            Kind = Kind,
            StringValue = StringValue,
            NumberValue = NumberValue,
            BooleanValue = BooleanValue,
            StringListValue = [.. StringListValue],
            NumberListValue = [.. NumberListValue],
            ContextRefValue = ContextRefValue,
            NamedSetRefValue = NamedSetRefValue,
            NamedSetBandMatchModeValue = NamedSetBandMatchModeValue
        };
    }

    public static RuleOperand ForString(string value) => new() { Kind = RuleValueType.String, StringValue = value };

    public static RuleOperand ForNumber(double value) => new() { Kind = RuleValueType.Number, NumberValue = value };

    public static RuleOperand ForBoolean(bool value) => new() { Kind = RuleValueType.Boolean, BooleanValue = value };

    public static RuleOperand ForStringList(IEnumerable<string> values) => new() { Kind = RuleValueType.StringList, StringListValue = [.. values] };

    public static RuleOperand ForNumberList(IEnumerable<double> values) => new() { Kind = RuleValueType.NumberList, NumberListValue = [.. values] };

    public static RuleOperand ForContextRef(RuleContextRef contextRef) => new() { Kind = RuleValueType.ContextRef, ContextRefValue = contextRef };

    public static RuleOperand ForNamedSet(RuleNamedSetRef namedSetRef, NamedSetBandMatchMode bandMatchMode = NamedSetBandMatchMode.MatchBand)
    {
        return new RuleOperand
        {
            Kind = RuleValueType.NamedSetRef,
            NamedSetRefValue = namedSetRef,
            NamedSetBandMatchModeValue = bandMatchMode
        };
    }
}

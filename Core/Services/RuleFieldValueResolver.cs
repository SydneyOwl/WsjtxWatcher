using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.Services;

public sealed class RuleFieldValueResolver
{
    public object? Resolve(RuleField field, RuleEvaluationContext context)
    {
        return field switch
        {
            RuleField.MessageText => context.Message?.Message,
            RuleField.TransmitterCallsign => context.Message?.Transmitter,
            RuleField.ReceiverCallsign => context.Message?.Receiver,
            RuleField.Mode => context.Message?.Mode,
            RuleField.Snr => context.Message?.Snr,
            RuleField.OffsetFrequencyHz => context.Message?.OffsetFrequencyHz,
            RuleField.OffsetTimeSeconds => context.Message?.OffsetTimeSeconds,
            RuleField.DialFrequencyHz => context.Message?.DialFrequencyHz,
            RuleField.CurrentBand => context.CurrentBand,
            RuleField.TransmitterGrid => context.Message?.TransmitterGrid,
            RuleField.FromCountryId => context.Message?.FromCountryId,
            RuleField.ToCountryId => context.Message?.ToCountryId,
            RuleField.IsLowConfidence => context.Message?.LowConfidence,
            RuleField.IsOffAir => context.Message?.OffAir,
            RuleField.IsUserTransmit => context.Message?.IsUserTransmit,
            RuleField.IsSystemNotice => context.Message?.IsSystemNotice,
            RuleField.LoggedQsoCallsign => context.LoggedQso is null ? string.Empty : IgnoredCallsignMatcher.NormalizeCallsign(context.LoggedQso.DxCall),
            RuleField.LoggedQsoBand => context.CurrentBand,
            _ => null
        };
    }

    public object? ResolveContextRef(RuleContextRef contextRef, RuleEvaluationContext context)
    {
        return contextRef switch
        {
            RuleContextRef.MyCallsign => context.Settings.MyCallsign,
            RuleContextRef.MyGrid => context.Settings.MyGrid,
            _ => null
        };
    }
}

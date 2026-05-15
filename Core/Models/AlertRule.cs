namespace WsjtxWatcher.Core.Models;

public sealed class AlertRule
{
    public string Id { get; set; } = string.Empty;
    public AlertRuleKind Kind { get; set; }
    public bool IsEnabled { get; set; }
    public bool SendNotification { get; set; }
    public bool Vibrate { get; set; }
    public int CooldownSeconds { get; set; } = 10;
    public AlertRuleMatchTarget MatchTarget { get; set; } = AlertRuleMatchTarget.TransmitterOnly;
    public List<string> CallsignPatterns { get; set; } = [];
    public HashSet<int> SelectedDxccIds { get; set; } = [];

    public AlertRule Clone()
    {
        return new AlertRule
        {
            Id = Id,
            Kind = Kind,
            IsEnabled = IsEnabled,
            SendNotification = SendNotification,
            Vibrate = Vibrate,
            CooldownSeconds = CooldownSeconds,
            MatchTarget = MatchTarget,
            CallsignPatterns = [.. CallsignPatterns],
            SelectedDxccIds = new HashSet<int>(SelectedDxccIds)
        };
    }
}

namespace WsjtxWatcher.Core.Models;

public sealed class RuleActionConfig
{
    public bool SendNotification { get; set; }
    public bool Vibrate { get; set; }
    public string NotificationTitleTemplate { get; set; } = string.Empty;
    public string NotificationBodyTemplate { get; set; } = string.Empty;
    public int HighlightColorArgb { get; set; }

    public RuleActionConfig Clone()
    {
        return new RuleActionConfig
        {
            SendNotification = SendNotification,
            Vibrate = Vibrate,
            NotificationTitleTemplate = NotificationTitleTemplate,
            NotificationBodyTemplate = NotificationBodyTemplate,
            HighlightColorArgb = HighlightColorArgb
        };
    }
}

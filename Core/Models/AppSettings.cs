namespace WsjtxWatcher.Core.Models;

public sealed class AppSettings
{
    public string Port { get; set; } = "2237";
    public string MyCallsign { get; set; } = string.Empty;
    public string MyGrid { get; set; } = string.Empty;
    public bool NotifyOnMyCall { get; set; }
    public bool NotifyOnAnyMessage { get; set; }
    public bool NotifyOnSelectedDxcc { get; set; }
    public bool VibrateOnMyCall { get; set; }
    public bool VibrateOnAnyMessage { get; set; }
    public bool VibrateOnSelectedDxcc { get; set; }
    public HashSet<int> PreferredDxccIds { get; set; } = new(DefaultPreferredDxccIds);

    public static IReadOnlyCollection<int> DefaultPreferredDxccIds { get; } = new[]
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

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Port = Port,
            MyCallsign = MyCallsign,
            MyGrid = MyGrid,
            NotifyOnMyCall = NotifyOnMyCall,
            NotifyOnAnyMessage = NotifyOnAnyMessage,
            NotifyOnSelectedDxcc = NotifyOnSelectedDxcc,
            VibrateOnMyCall = VibrateOnMyCall,
            VibrateOnAnyMessage = VibrateOnAnyMessage,
            VibrateOnSelectedDxcc = VibrateOnSelectedDxcc,
            PreferredDxccIds = new HashSet<int>(PreferredDxccIds)
        };
    }
}

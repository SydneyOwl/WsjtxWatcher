namespace WsjtxWatcher.Core.Models;

public sealed class AppSettings
{
    public DataSourceType DataSourceType { get; set; } = DataSourceType.Udp;
    public string Port { get; set; } = "2237";
    public string RelayServerUrl { get; set; } = string.Empty;
    public string RelaySharedSecret { get; set; } = string.Empty;
    public string RelayTenantId { get; set; } = string.Empty;
    public string RelayPreferredSourceName { get; set; } = string.Empty;
    public string RelayTrustedFingerprint { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string MyCallsign { get; set; } = string.Empty;
    public string MyGrid { get; set; } = string.Empty;
    public List<string> WatchedCallsignPatterns { get; set; } = [];
    public bool NotifyOnMyCall { get; set; }
    public bool NotifyOnAnyMessage { get; set; }
    public bool NotifyOnSelectedDxcc { get; set; }
    public bool NotifyOnLoggedQso { get; set; }
    public bool VibrateOnMyCall { get; set; }
    public bool VibrateOnAnyMessage { get; set; }
    public bool VibrateOnSelectedDxcc { get; set; }
    public bool VibrateOnLoggedQso { get; set; }
    public bool AutoIgnoreLoggedQso { get; set; } = true;
    public IgnoredCallsignMatchTarget IgnoredCallsignMatchTarget { get; set; } = IgnoredCallsignMatchTarget.TransmitterOnly;
    public WatchedCallsignMatchTarget WatchedCallsignMatchTarget { get; set; } = WatchedCallsignMatchTarget.TransmitterOnly;
    public SelectedDxccMatchTarget SelectedDxccMatchTarget { get; set; } = SelectedDxccMatchTarget.TransmitterOnly;
    public AppTheme Theme { get; set; } = AppTheme.FollowSystem;
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
        var clone = new AppSettings
        {
            DataSourceType = DataSourceType,
            Port = Port,
            RelayServerUrl = RelayServerUrl,
            RelaySharedSecret = RelaySharedSecret,
            RelayTenantId = RelayTenantId,
            RelayPreferredSourceName = RelayPreferredSourceName,
            RelayTrustedFingerprint = RelayTrustedFingerprint,
            Language = Language,
            MyCallsign = MyCallsign,
            MyGrid = MyGrid,
            WatchedCallsignPatterns = [.. WatchedCallsignPatterns],
            NotifyOnMyCall = NotifyOnMyCall,
            NotifyOnAnyMessage = NotifyOnAnyMessage,
            NotifyOnSelectedDxcc = NotifyOnSelectedDxcc,
            NotifyOnLoggedQso = NotifyOnLoggedQso,
            VibrateOnMyCall = VibrateOnMyCall,
            VibrateOnAnyMessage = VibrateOnAnyMessage,
            VibrateOnSelectedDxcc = VibrateOnSelectedDxcc,
            VibrateOnLoggedQso = VibrateOnLoggedQso,
            Theme = Theme,
            AutoIgnoreLoggedQso = AutoIgnoreLoggedQso,
            IgnoredCallsignMatchTarget = IgnoredCallsignMatchTarget,
            WatchedCallsignMatchTarget = WatchedCallsignMatchTarget,
            SelectedDxccMatchTarget = SelectedDxccMatchTarget,
            PreferredDxccIds = new HashSet<int>(PreferredDxccIds)
        };
        return clone;
    }
}

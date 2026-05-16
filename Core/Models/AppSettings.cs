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
    public List<AlertRule> AlertRules { get; set; } = [];
    public bool AutoIgnoreLoggedQso { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.FollowSystem;

    public AppSettings Clone()
    {
        return new AppSettings
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
            AlertRules = AlertRuleCatalog.NormalizeRules(AlertRules),
            Theme = Theme,
            AutoIgnoreLoggedQso = AutoIgnoreLoggedQso
        };
    }
}

namespace WsjtxWatcher.Core.Models;

public sealed class CloudlogImportSettings
{
    public string Url { get; set; } = string.Empty;
    public string StationId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int LookbackDays { get; set; } = 365 * 100;
}

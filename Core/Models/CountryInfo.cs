namespace WsjtxWatcher.Core.Models;

public sealed class CountryInfo
{
    public int Id { get; init; }
    public string EnglishName { get; init; } = string.Empty;
    public string ChineseName { get; init; } = string.Empty;
    public int CqZone { get; init; }
    public int ItuZone { get; init; }
    public string Continent { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double GmtOffset { get; init; }
    public string DxccPrefix { get; init; } = string.Empty;

    public string DisplayName(string languageCode)
    {
        return languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ChineseName)
            ? ChineseName
            : EnglishName;
    }
}

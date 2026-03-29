namespace WsjtxWatcher.Core.Models;

public sealed class CountrySelectionItem
{
    public CountryInfo Country { get; init; } = new();
    public bool IsSelected { get; set; }
}

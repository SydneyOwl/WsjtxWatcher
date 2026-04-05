using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WsjtxWatcher.Core.Models;

public sealed class RelaySourceDescriptor
{
    public string SourceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Online { get; set; }
    public long LastSeenUnixMs { get; set; }
}

public partial class RelayRuntimeState : ObservableObject
{
    [ObservableProperty]
    private bool isRelayMode;

    [ObservableProperty]
    private bool isConnecting;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string connectionStatus = string.Empty;

    [ObservableProperty]
    private string currentSourceName = string.Empty;

    [ObservableProperty]
    private bool currentSourceOnline;

    [ObservableProperty]
    private string serverFingerprint = string.Empty;

    [ObservableProperty]
    private string lastNotice = string.Empty;

    public ObservableCollection<RelaySourceDescriptor> Sources { get; } = new();

    public void Reset(bool relayMode = false)
    {
        IsRelayMode = relayMode;
        IsConnecting = false;
        IsConnected = false;
        ConnectionStatus = string.Empty;
        CurrentSourceName = string.Empty;
        CurrentSourceOnline = false;
        ServerFingerprint = string.Empty;
        LastNotice = string.Empty;
        Sources.Clear();
    }

    public void SetFingerprint(string fingerprint)
    {
        ServerFingerprint = fingerprint ?? string.Empty;
    }

    public void SetConnectionState(bool connecting, bool connected, string status)
    {
        IsRelayMode = true;
        IsConnecting = connecting;
        IsConnected = connected;
        ConnectionStatus = status ?? string.Empty;
    }

    public void UpdateCatalog(IEnumerable<RelaySourceDescriptor> sources, string currentSourceName)
    {
        Sources.Clear();
        foreach (var source in sources)
        {
            Sources.Add(new RelaySourceDescriptor
            {
                SourceName = source.SourceName,
                DisplayName = source.DisplayName,
                Online = source.Online,
                LastSeenUnixMs = source.LastSeenUnixMs
            });
        }

        if (!string.IsNullOrWhiteSpace(currentSourceName))
        {
            CurrentSourceName = currentSourceName;
            var selected = Sources.FirstOrDefault(item => string.Equals(item.SourceName, currentSourceName, StringComparison.OrdinalIgnoreCase));
            CurrentSourceOnline = selected?.Online ?? false;
        }
        else if (Sources.All(item => !string.Equals(item.SourceName, CurrentSourceName, StringComparison.OrdinalIgnoreCase)))
        {
            CurrentSourceOnline = false;
        }
    }

    public void ApplySnapshot(string sourceName, bool sourceOnline)
    {
        CurrentSourceName = sourceName ?? string.Empty;
        CurrentSourceOnline = sourceOnline;
    }

    public void ApplySourceState(string sourceName, bool online, string message)
    {
        var existing = Sources.FirstOrDefault(item => string.Equals(item.SourceName, sourceName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Online = online;
            existing.LastSeenUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        if (string.Equals(CurrentSourceName, sourceName, StringComparison.OrdinalIgnoreCase))
        {
            CurrentSourceOnline = online;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            LastNotice = message;
        }
    }
}

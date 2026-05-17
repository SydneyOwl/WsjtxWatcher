namespace WsjtxWatcher.Core.Models;

public sealed record RelaySourceCatalogResult(
    bool Success,
    string Message,
    IReadOnlyList<RelaySourceDescriptor> Sources,
    string CurrentSourceName = "",
    string ObservedFingerprint = "");

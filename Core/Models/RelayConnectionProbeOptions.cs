namespace WsjtxWatcher.Core.Models;

public sealed record RelayConnectionProbeOptions(
    string ServerUrl,
    string SharedSecret,
    string TenantId,
    string TrustedFingerprint);

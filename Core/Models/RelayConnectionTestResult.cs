namespace WsjtxWatcher.Core.Models;

public sealed record RelayConnectionTestResult(
    bool Success,
    string Message,
    string ObservedFingerprint = "");

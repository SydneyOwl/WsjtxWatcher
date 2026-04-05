using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface IRelayConnectionProbe
{
    Task<RelayConnectionTestResult> TestWatchConnectionAsync(
        RelayConnectionProbeOptions options,
        CancellationToken cancellationToken = default);
}

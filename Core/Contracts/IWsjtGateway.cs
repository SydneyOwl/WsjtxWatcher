using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface IWsjtGateway
{
    bool IsRunning { get; }
    Task StartAsync(int port, IWsjtEventSink eventSink, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IWsjtEventSink
{
    Task OnSessionActivityAsync(WsjtSessionEvent sessionEvent, CancellationToken cancellationToken = default);
    Task OnDecodeAsync(WsjtDecodeEvent decodeEvent, CancellationToken cancellationToken = default);
    Task OnStatusAsync(WsjtStatusEvent statusEvent, CancellationToken cancellationToken = default);
}

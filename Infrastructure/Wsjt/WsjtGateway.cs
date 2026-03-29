using System.Net;
using Serilog;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Wsjt;

public sealed class WsjtGateway : IWsjtGateway
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private WsjtxUdpServer? _server;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsRunning => _server?.IsRunning ?? false;

    public async Task StartAsync(int port, IWsjtEventSink eventSink, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
            _cancellationTokenSource = new CancellationTokenSource();
            var handler = new WsjtMessageHandler(eventSink);
            _server = new WsjtxUdpServer(handler, IPAddress.Any, port);
            Log.Information("Starting WSJT-X UDP listener on {Endpoint}", _server.LocalEndpoint);
            _server.Start(_cancellationTokenSource);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task StopCoreAsync()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            if (_server?.IsRunning == true)
            {
                _server.Stop();
            }

            if (_server is not null && !_server.IsDisposed)
            {
                _server.Dispose();
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to stop WSJT-X UDP listener cleanly.");
        }
        finally
        {
            _server = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        return Task.CompletedTask;
    }
}

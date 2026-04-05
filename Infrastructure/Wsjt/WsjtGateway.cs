using System.Net;
using Serilog;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Wsjt;

public sealed class UdpWsjtGateway : IWsjtGateway
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private WsjtxUdpServer? _server;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsRunning => _server?.IsRunning ?? false;

    public async Task StartAsync(AppSettings settings, IWsjtEventSink eventSink, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
            _cancellationTokenSource = new CancellationTokenSource();
            var handler = new WsjtMessageHandler(eventSink);
            var port = ParsePort(settings.Port);
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

    public Task SelectSourceAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private Task StopCoreAsync()
    {
        var server = _server;
        var cancellationTokenSource = _cancellationTokenSource;
        _server = null;
        _cancellationTokenSource = null;

        if (server is null && cancellationTokenSource is null)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            try
            {
                cancellationTokenSource?.Cancel();
                if (server?.IsRunning == true)
                {
                    server.Stop();
                }

                if (server is not null && !server.IsDisposed)
                {
                    server.Dispose();
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to stop WSJT-X UDP listener cleanly.");
            }
            finally
            {
                cancellationTokenSource?.Dispose();
            }
        });
    }

    private static int ParsePort(string value)
    {
        return int.TryParse(value, out var port) && port is > 0 and < 65536
            ? port
            : throw new InvalidOperationException($"Invalid WSJT-X port: {value}");
    }
}

using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Wsjt;

public sealed class WsjtGateway : IWsjtGateway
{
    private readonly UdpWsjtGateway _udpGateway;
    private readonly RelayWsjtGateway _relayGateway;
    private readonly RelayRuntimeState _relayRuntimeState;
    private IWsjtGateway? _activeGateway;

    public WsjtGateway(UdpWsjtGateway udpGateway, RelayWsjtGateway relayGateway, RelayRuntimeState relayRuntimeState)
    {
        _udpGateway = udpGateway;
        _relayGateway = relayGateway;
        _relayRuntimeState = relayRuntimeState;
    }

    public bool IsRunning => _activeGateway?.IsRunning ?? false;

    public async Task StartAsync(AppSettings settings, IWsjtEventSink eventSink, CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        _activeGateway = settings.DataSourceType == DataSourceType.Relay ? _relayGateway : _udpGateway;
        if (settings.DataSourceType != DataSourceType.Relay)
        {
            _relayRuntimeState.Reset();
        }

        await _activeGateway.StartAsync(settings, eventSink, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_activeGateway is not null)
        {
            await _activeGateway.StopAsync(cancellationToken).ConfigureAwait(false);
            _activeGateway = null;
        }
    }

    public Task SelectSourceAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        return _activeGateway?.SelectSourceAsync(sourceName, cancellationToken) ?? Task.CompletedTask;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return _activeGateway?.RefreshAsync(cancellationToken) ?? Task.CompletedTask;
    }
}

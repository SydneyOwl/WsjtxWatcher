using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Services;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class RelaySourceSelectionViewModel
{
    private readonly IRelayConnectionProbe _relayConnectionProbe;
    private readonly ISettingsStore _settingsStore;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly WatcherController _watcherController;

    public RelaySourceSelectionViewModel(
        ISettingsStore settingsStore,
        IRelayConnectionProbe relayConnectionProbe,
        IUiDispatcher uiDispatcher,
        WatcherController watcherController,
        RelayRuntimeState relayRuntimeState)
    {
        _settingsStore = settingsStore;
        _relayConnectionProbe = relayConnectionProbe;
        _uiDispatcher = uiDispatcher;
        _watcherController = watcherController;
        RelayRuntimeState = relayRuntimeState;
    }

    public RelayRuntimeState RelayRuntimeState { get; }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_watcherController.State.IsServiceRunning)
        {
            await _watcherController.RefreshGatewayAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var result = await _relayConnectionProbe
            .GetWatchSourceCatalogAsync(new RelayConnectionProbeOptions(
                settings.RelayServerUrl,
                settings.RelaySharedSecret,
                settings.RelayTenantId,
                settings.RelayTrustedFingerprint), cancellationToken)
            .ConfigureAwait(false);

        await _uiDispatcher.InvokeAsync(() =>
        {
            RelayRuntimeState.SetFingerprint(settings.RelayTrustedFingerprint);
            RelayRuntimeState.SetConnectionState(connecting: false, connected: result.Success, status: result.Message);
            RelayRuntimeState.UpdateCatalog(result.Sources, result.CurrentSourceName);
            if (!result.Success)
            {
                RelayRuntimeState.LastNotice = result.Message;
            }
        }).ConfigureAwait(false);
    }

    public async Task SelectAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.RelayPreferredSourceName = sourceName?.Trim() ?? string.Empty;
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        if (_watcherController.State.IsServiceRunning)
        {
            await _watcherController.SwitchRelaySourceAsync(settings.RelayPreferredSourceName, cancellationToken).ConfigureAwait(false);
        }
    }
}

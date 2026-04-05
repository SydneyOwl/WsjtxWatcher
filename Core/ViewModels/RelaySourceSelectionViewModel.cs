using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Services;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class RelaySourceSelectionViewModel
{
    private readonly ISettingsStore _settingsStore;
    private readonly WatcherController _watcherController;

    public RelaySourceSelectionViewModel(ISettingsStore settingsStore, WatcherController watcherController, RelayRuntimeState relayRuntimeState)
    {
        _settingsStore = settingsStore;
        _watcherController = watcherController;
        RelayRuntimeState = relayRuntimeState;
    }

    public RelayRuntimeState RelayRuntimeState { get; }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return _watcherController.RefreshGatewayAsync(cancellationToken);
    }

    public async Task SelectAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.RelayPreferredSourceName = sourceName?.Trim() ?? string.Empty;
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.SwitchRelaySourceAsync(settings.RelayPreferredSourceName, cancellationToken).ConfigureAwait(false);
    }
}

using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class AlertRulesViewModel
{
    private readonly ISettingsStore _settingsStore;

    public AlertRulesViewModel(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public async Task<IReadOnlyList<AlertRule>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return AlertRuleCatalog.GetAllRules(settings);
    }
}

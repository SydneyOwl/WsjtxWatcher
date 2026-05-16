using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class AlertRulesViewModel
{
    private readonly ISettingsStore _settingsStore;
    private readonly Services.WatcherController _watcherController;

    public AlertRulesViewModel(ISettingsStore settingsStore, Services.WatcherController watcherController)
    {
        _settingsStore = settingsStore;
        _watcherController = watcherController;
    }

    public async Task<IReadOnlyList<AlertRule>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return AlertRuleCatalog.GetAllRules(settings);
    }

    public async Task SetEnabledAsync(string ruleId, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var rule = AlertRuleCatalog.GetRequiredRule(settings, ruleId).Clone();
        rule.IsEnabled = isEnabled;
        AlertRuleCatalog.UpsertRule(settings, rule);
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

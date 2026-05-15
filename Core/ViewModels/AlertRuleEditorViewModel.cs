using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class AlertRuleEditorViewModel
{
    private readonly ISettingsStore _settingsStore;
    private readonly Services.WatcherController _watcherController;

    public AlertRuleEditorViewModel(ISettingsStore settingsStore, Services.WatcherController watcherController)
    {
        _settingsStore = settingsStore;
        _watcherController = watcherController;
    }

    public async Task<AlertRule> LoadAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var rule = AlertRuleCatalog.GetRequiredRule(settings, ruleId);
        return rule.Clone();
    }

    public async Task SaveAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        AlertRuleCatalog.UpsertRule(settings, rule);
        settings.AlertRules = AlertRuleCatalog.Normalize(settings.AlertRules);
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

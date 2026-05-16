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
        return AlertRuleCatalog.GetRequiredRule(settings, ruleId).Clone();
    }

    public async Task<AlertRule> CreateAsync(RuleTriggerType triggerType, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var nextSortOrder = AlertRuleCatalog.NormalizeRules(settings.AlertRules)
            .Where(rule => rule.Source == RuleSource.UserDefined)
            .Select(rule => rule.SortOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        return AlertRuleCatalog.CreateCustomRule(triggerType, nextSortOrder);
    }

    public async Task SaveAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        AlertRuleCatalog.UpsertRule(settings, rule);
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!AlertRuleCatalog.RemoveRule(settings, ruleId))
        {
            return;
        }

        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

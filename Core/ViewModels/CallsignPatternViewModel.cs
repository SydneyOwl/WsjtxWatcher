using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.ViewModels;

public sealed class CallsignPatternViewModel
{
    private readonly ISettingsStore _settingsStore;
    private readonly Services.WatcherController _watcherController;

    public CallsignPatternViewModel(ISettingsStore settingsStore, Services.WatcherController watcherController)
    {
        _settingsStore = settingsStore;
        _watcherController = watcherController;
    }

    public async Task<IReadOnlyList<string>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var rule = AlertRuleCatalog.GetRequiredRule(settings, AlertRuleCatalog.WatchedCallsignRuleId);
        return AlertRuleCatalog.GetEffectiveCallsignPatterns(rule);
    }

    public async Task SaveAsync(IEnumerable<string> patterns, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var rule = AlertRuleCatalog.GetRequiredRule(settings, AlertRuleCatalog.WatchedCallsignRuleId);
        rule.CallsignPatterns = [.. CallsignPatternMatcher.NormalizePatterns(patterns)];
        AlertRuleCatalog.UpsertRule(settings, rule);
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

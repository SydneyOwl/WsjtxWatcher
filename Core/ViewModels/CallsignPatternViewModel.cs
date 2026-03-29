using WsjtxWatcher.Core.Contracts;
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
        return CallsignPatternMatcher.NormalizePatterns(settings.WatchedCallsignPatterns);
    }

    public async Task SaveAsync(IEnumerable<string> patterns, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.WatchedCallsignPatterns = [.. CallsignPatternMatcher.NormalizePatterns(patterns)];
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}

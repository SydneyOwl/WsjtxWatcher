using Android.App;
using Android.Content;
using System.Text.Json;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidSettingsStore : ISettingsStore
{
    private const string StorageKey = "8fdad8ad";
    private readonly IIgnoredCallsignStore _ignoredCallsignStore;
    private readonly ISharedPreferences _sharedPreferences;

    public AndroidSettingsStore(Application application, IIgnoredCallsignStore ignoredCallsignStore)
    {
        _sharedPreferences = application.GetSharedPreferences(StorageKey, FileCreationMode.Private)!;
        _ignoredCallsignStore = ignoredCallsignStore;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var preferredDxcc = _sharedPreferences.GetStringSet("preferred_dxcc", AppSettings.DefaultPreferredDxccIds.Select(id => id.ToString()).ToHashSet())
                            ?? AppSettings.DefaultPreferredDxccIds.Select(id => id.ToString()).ToHashSet();
        var callsign = _sharedPreferences.GetString("callsign", string.Empty) ?? string.Empty;
        var watchedPatterns = LoadWatchedCallsignPatterns(callsign);
        var ignoredCallsigns = await _ignoredCallsignStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var ignoredCallsignMatchTargetValue = _sharedPreferences.GetInt(
            "ignored_callsign_match_target",
            (int)IgnoredCallsignMatchTarget.TransmitterOnly);
        var ignoredCallsignMatchTarget = Enum.IsDefined(typeof(IgnoredCallsignMatchTarget), ignoredCallsignMatchTargetValue)
            ? (IgnoredCallsignMatchTarget)ignoredCallsignMatchTargetValue
            : IgnoredCallsignMatchTarget.TransmitterOnly;

        var settings = new AppSettings
        {
            Port = _sharedPreferences.GetString("port", "2237") ?? "2237",
            Language = _sharedPreferences.GetString("language", string.Empty) ?? string.Empty,
            MyCallsign = callsign,
            MyGrid = _sharedPreferences.GetString("grid", string.Empty) ?? string.Empty,
            WatchedCallsignPatterns = [.. watchedPatterns],
            IgnoredCallsigns = [.. ignoredCallsigns],
            NotifyOnMyCall = _sharedPreferences.GetBoolean("notify_on_my_call", false),
            NotifyOnAnyMessage = _sharedPreferences.GetBoolean("notify_on_any", false),
            NotifyOnSelectedDxcc = _sharedPreferences.GetBoolean("notify_on_dxcc", false),
            NotifyOnLoggedQso = _sharedPreferences.GetBoolean("notify_on_logged_qso", false),
            VibrateOnMyCall = _sharedPreferences.GetBoolean("vibrate_on_my_call", false),
            VibrateOnAnyMessage = _sharedPreferences.GetBoolean("vibrate_on_any", false),
            VibrateOnSelectedDxcc = _sharedPreferences.GetBoolean("vibrate_on_dxcc", false),
            AutoIgnoreLoggedQso = _sharedPreferences.GetBoolean("auto_ignore_logged_qso", true),
            IgnoredCallsignMatchTarget = ignoredCallsignMatchTarget,
            PreferredDxccIds = new HashSet<int>(preferredDxcc.Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0))
        };

        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.PutString("port", settings.Port);
        editor.PutString("language", settings.Language);
        editor.PutString("callsign", settings.MyCallsign);
        editor.PutString("grid", settings.MyGrid);
        editor.PutString("callsign_patterns", JsonSerializer.Serialize(CallsignPatternMatcher.NormalizePatterns(settings.WatchedCallsignPatterns)));
        editor.PutBoolean("notify_on_my_call", settings.NotifyOnMyCall);
        editor.PutBoolean("notify_on_any", settings.NotifyOnAnyMessage);
        editor.PutBoolean("notify_on_dxcc", settings.NotifyOnSelectedDxcc);
        editor.PutBoolean("notify_on_logged_qso", settings.NotifyOnLoggedQso);
        editor.PutBoolean("vibrate_on_my_call", settings.VibrateOnMyCall);
        editor.PutBoolean("vibrate_on_any", settings.VibrateOnAnyMessage);
        editor.PutBoolean("vibrate_on_dxcc", settings.VibrateOnSelectedDxcc);
        editor.PutBoolean("auto_ignore_logged_qso", settings.AutoIgnoreLoggedQso);
        editor.PutInt("ignored_callsign_match_target", (int)settings.IgnoredCallsignMatchTarget);
        editor.PutStringSet("preferred_dxcc", settings.PreferredDxccIds.Select(id => id.ToString()).ToHashSet());
        editor.Remove("ignored_callsigns");
        editor.Apply();
        await _ignoredCallsignStore.SaveAsync(settings.IgnoredCallsigns, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.Clear();
        editor.Apply();
        await _ignoredCallsignStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await SaveAsync(new AppSettings(), cancellationToken).ConfigureAwait(false);
    }

    private List<string> LoadWatchedCallsignPatterns(string callsign)
    {
        var json = _sharedPreferences.GetString("callsign_patterns", string.Empty) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var patterns = JsonSerializer.Deserialize<List<string>>(json);
                return [.. CallsignPatternMatcher.NormalizePatterns(patterns)];
            }
            catch (JsonException)
            {
            }
        }

        if (string.IsNullOrWhiteSpace(callsign))
        {
            return [];
        }

        return [CallsignPatternMatcher.CreateDefaultPattern(callsign)];
    }
}

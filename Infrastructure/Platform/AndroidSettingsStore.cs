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
    private readonly ISharedPreferences _sharedPreferences;

    public AndroidSettingsStore(Application application)
    {
        _sharedPreferences = application.GetSharedPreferences(StorageKey, FileCreationMode.Private)!;
    }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var preferredDxcc = _sharedPreferences.GetStringSet("preferred_dxcc", AppSettings.DefaultPreferredDxccIds.Select(id => id.ToString()).ToHashSet())
                            ?? AppSettings.DefaultPreferredDxccIds.Select(id => id.ToString()).ToHashSet();
        var callsign = _sharedPreferences.GetString("callsign", string.Empty) ?? string.Empty;
        var watchedPatterns = LoadWatchedCallsignPatterns(callsign);

        var settings = new AppSettings
        {
            Port = _sharedPreferences.GetString("port", "2237") ?? "2237",
            Language = _sharedPreferences.GetString("language", string.Empty) ?? string.Empty,
            MyCallsign = callsign,
            MyGrid = _sharedPreferences.GetString("grid", string.Empty) ?? string.Empty,
            WatchedCallsignPatterns = [.. watchedPatterns],
            NotifyOnMyCall = _sharedPreferences.GetBoolean("notify_on_my_call", false),
            NotifyOnAnyMessage = _sharedPreferences.GetBoolean("notify_on_any", false),
            NotifyOnSelectedDxcc = _sharedPreferences.GetBoolean("notify_on_dxcc", false),
            VibrateOnMyCall = _sharedPreferences.GetBoolean("vibrate_on_my_call", false),
            VibrateOnAnyMessage = _sharedPreferences.GetBoolean("vibrate_on_any", false),
            VibrateOnSelectedDxcc = _sharedPreferences.GetBoolean("vibrate_on_dxcc", false),
            PreferredDxccIds = new HashSet<int>(preferredDxcc.Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0))
        };

        return Task.FromResult(settings);
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
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
        editor.PutBoolean("vibrate_on_my_call", settings.VibrateOnMyCall);
        editor.PutBoolean("vibrate_on_any", settings.VibrateOnAnyMessage);
        editor.PutBoolean("vibrate_on_dxcc", settings.VibrateOnSelectedDxcc);
        editor.PutStringSet("preferred_dxcc", settings.PreferredDxccIds.Select(id => id.ToString()).ToHashSet());
        editor.Apply();
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.Clear();
        editor.Apply();
        return SaveAsync(new AppSettings(), cancellationToken);
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

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
        var ignoredCallsignMatchTargetValue = _sharedPreferences.GetInt(
            "ignored_callsign_match_target",
            (int)IgnoredCallsignMatchTarget.TransmitterOnly);
        var ignoredCallsignMatchTarget = Enum.IsDefined(typeof(IgnoredCallsignMatchTarget), ignoredCallsignMatchTargetValue)
            ? (IgnoredCallsignMatchTarget)ignoredCallsignMatchTargetValue
            : IgnoredCallsignMatchTarget.TransmitterOnly;
        var watchedCallsignMatchTargetValue = _sharedPreferences.GetInt(
            "watched_callsign_match_target",
            (int)WatchedCallsignMatchTarget.TransmitterOnly);
        var watchedCallsignMatchTarget = Enum.IsDefined(typeof(WatchedCallsignMatchTarget), watchedCallsignMatchTargetValue)
            ? (WatchedCallsignMatchTarget)watchedCallsignMatchTargetValue
            : WatchedCallsignMatchTarget.TransmitterOnly;
        var selectedDxccMatchTargetValue = _sharedPreferences.GetInt(
            "selected_dxcc_match_target",
            (int)SelectedDxccMatchTarget.TransmitterOnly);
        var selectedDxccMatchTarget = Enum.IsDefined(typeof(SelectedDxccMatchTarget), selectedDxccMatchTargetValue)
            ? (SelectedDxccMatchTarget)selectedDxccMatchTargetValue
            : SelectedDxccMatchTarget.TransmitterOnly;

        var settings = new AppSettings
        {
            DataSourceType = ParseDataSourceType(_sharedPreferences.GetInt("data_source_type", (int)DataSourceType.Udp)),
            Port = _sharedPreferences.GetString("port", "2237") ?? "2237",
            RelayServerUrl = _sharedPreferences.GetString("relay_server_url", string.Empty) ?? string.Empty,
            RelaySharedSecret = _sharedPreferences.GetString("relay_shared_secret", string.Empty) ?? string.Empty,
            RelayTenantId = _sharedPreferences.GetString("relay_tenant_id", string.Empty) ?? string.Empty,
            RelayPreferredSourceName = _sharedPreferences.GetString("relay_preferred_source_name", string.Empty) ?? string.Empty,
            RelayTrustedFingerprint = _sharedPreferences.GetString("relay_trusted_fingerprint", string.Empty) ?? string.Empty,
            Language = _sharedPreferences.GetString("language", string.Empty) ?? string.Empty,
            MyCallsign = callsign,
            MyGrid = _sharedPreferences.GetString("grid", string.Empty) ?? string.Empty,
            WatchedCallsignPatterns = [.. watchedPatterns],
            NotifyOnMyCall = _sharedPreferences.GetBoolean("notify_on_my_call", false),
            NotifyOnAnyMessage = _sharedPreferences.GetBoolean("notify_on_any", false),
            NotifyOnSelectedDxcc = _sharedPreferences.GetBoolean("notify_on_dxcc", false),
            NotifyOnLoggedQso = _sharedPreferences.GetBoolean("notify_on_logged_qso", false),
            VibrateOnMyCall = _sharedPreferences.GetBoolean("vibrate_on_my_call", false),
            VibrateOnAnyMessage = _sharedPreferences.GetBoolean("vibrate_on_any", false),
            VibrateOnSelectedDxcc = _sharedPreferences.GetBoolean("vibrate_on_dxcc", false),
            VibrateOnLoggedQso = _sharedPreferences.GetBoolean("vibrate_on_logged_qso", false),
            AutoIgnoreLoggedQso = _sharedPreferences.GetBoolean("auto_ignore_logged_qso", true),
            IgnoredCallsignMatchTarget = ignoredCallsignMatchTarget,
            WatchedCallsignMatchTarget = watchedCallsignMatchTarget,
            SelectedDxccMatchTarget = selectedDxccMatchTarget,
            PreferredDxccIds = new HashSet<int>(preferredDxcc.Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0))
        };

        return Task.FromResult(settings);
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.PutInt("data_source_type", (int)settings.DataSourceType);
        editor.PutString("port", settings.Port);
        editor.PutString("relay_server_url", settings.RelayServerUrl);
        editor.PutString("relay_shared_secret", settings.RelaySharedSecret);
        editor.PutString("relay_tenant_id", settings.RelayTenantId);
        editor.PutString("relay_preferred_source_name", settings.RelayPreferredSourceName);
        editor.PutString("relay_trusted_fingerprint", settings.RelayTrustedFingerprint);
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
        editor.PutBoolean("vibrate_on_logged_qso", settings.VibrateOnLoggedQso);
        editor.PutBoolean("auto_ignore_logged_qso", settings.AutoIgnoreLoggedQso);
        editor.PutInt("ignored_callsign_match_target", (int)settings.IgnoredCallsignMatchTarget);
        editor.PutInt("watched_callsign_match_target", (int)settings.WatchedCallsignMatchTarget);
        editor.PutInt("selected_dxcc_match_target", (int)settings.SelectedDxccMatchTarget);
        editor.PutStringSet("preferred_dxcc", settings.PreferredDxccIds.Select(id => id.ToString()).ToHashSet());
        if (!editor.Commit())
        {
            throw new InvalidOperationException("Failed to persist application settings.");
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.Clear();
        if (!editor.Commit())
        {
            throw new InvalidOperationException("Failed to clear application settings.");
        }
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

    private static DataSourceType ParseDataSourceType(int value)
    {
        return Enum.IsDefined(typeof(DataSourceType), value)
            ? (DataSourceType)value
            : DataSourceType.Udp;
    }
}

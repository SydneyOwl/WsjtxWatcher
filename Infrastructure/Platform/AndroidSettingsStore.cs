using Android.App;
using Android.Content;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidSettingsStore : ISettingsStore
{
    private const string StorageKey = "8fdad8ad";
    private readonly ISharedPreferences _sharedPreferences;

    public AndroidSettingsStore(Application application, IAppInfoService appInfoService)
    {
        _sharedPreferences = application.GetSharedPreferences(StorageKey, FileCreationMode.Private)!;
    }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var preferredDxcc = _sharedPreferences.GetStringSet("preferred_dxcc", AppSettings.DefaultPreferredDxccIds.Select(id => id.ToString()).ToHashSet())
                            ?? AppSettings.DefaultPreferredDxccIds.Select(id => id.ToString()).ToHashSet();

        var settings = new AppSettings
        {
            Port = _sharedPreferences.GetString("port", "2237") ?? "2237",
            MyCallsign = _sharedPreferences.GetString("callsign", string.Empty) ?? string.Empty,
            MyGrid = _sharedPreferences.GetString("grid", string.Empty) ?? string.Empty,
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
        editor.PutString("callsign", settings.MyCallsign);
        editor.PutString("grid", settings.MyGrid);
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
}

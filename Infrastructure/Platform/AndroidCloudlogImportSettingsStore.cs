using Android.App;
using Android.Content;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidCloudlogImportSettingsStore : ICloudlogImportSettingsStore
{
    private const string StorageKey = "cloudlog_import_settings";
    private readonly ISharedPreferences _sharedPreferences;

    public AndroidCloudlogImportSettingsStore(Application application)
    {
        _sharedPreferences = application.GetSharedPreferences(StorageKey, FileCreationMode.Private)!;
    }

    public Task<CloudlogImportSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = new CloudlogImportSettings
        {
            Url = _sharedPreferences.GetString("url", string.Empty) ?? string.Empty,
            StationId = _sharedPreferences.GetString("station_id", string.Empty) ?? string.Empty,
            Username = _sharedPreferences.GetString("username", string.Empty) ?? string.Empty,
            Password = _sharedPreferences.GetString("password", string.Empty) ?? string.Empty,
            LookbackDays = _sharedPreferences.GetInt("lookback_days", 365 * 100)
        };
        return Task.FromResult(settings);
    }

    public Task SaveAsync(CloudlogImportSettings settings, CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.PutString("url", settings.Url);
        editor.PutString("station_id", settings.StationId);
        editor.PutString("username", settings.Username);
        editor.PutString("password", settings.Password);
        editor.PutInt("lookback_days", settings.LookbackDays);
        editor.Apply();
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var editor = _sharedPreferences.Edit()!;
        editor.Clear();
        editor.Apply();
        return Task.CompletedTask;
    }
}

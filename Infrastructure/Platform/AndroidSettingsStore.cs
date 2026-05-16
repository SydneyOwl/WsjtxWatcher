using Android.App;
using Android.Content;
using System.Text.Json;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidSettingsStore : ISettingsStore
{
    private const string StorageKey = "8fdad8ad";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ISharedPreferences _sharedPreferences;

    public AndroidSettingsStore(Application application)
    {
        _sharedPreferences = application.GetSharedPreferences(StorageKey, FileCreationMode.Private)!;
    }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
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
            MyCallsign = _sharedPreferences.GetString("callsign", string.Empty) ?? string.Empty,
            MyGrid = _sharedPreferences.GetString("grid", string.Empty) ?? string.Empty,
            AlertRules = LoadAlertRules(),
            AutoIgnoreLoggedQso = _sharedPreferences.GetBoolean("auto_ignore_logged_qso", true),
            Theme = ParseTheme(_sharedPreferences.GetInt("theme", 0))
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
        editor.PutString("alert_rules", JsonSerializer.Serialize(AlertRuleCatalog.NormalizeRules(settings.AlertRules), JsonOptions));
        editor.PutBoolean("auto_ignore_logged_qso", settings.AutoIgnoreLoggedQso);
        editor.PutInt("theme", (int)settings.Theme);
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

    private List<AlertRule> LoadAlertRules()
    {
        var json = _sharedPreferences.GetString("alert_rules", string.Empty) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var rules = JsonSerializer.Deserialize<List<AlertRule>>(json, JsonOptions);
                return AlertRuleCatalog.NormalizeRules(rules);
            }
            catch (JsonException)
            {
            }
        }

        return AlertRuleCatalog.CreateSystemRules().Select(rule => rule.Clone()).ToList();
    }

    private static AppTheme ParseTheme(int value)
    {
        return Enum.IsDefined(typeof(AppTheme), value)
            ? (AppTheme)value
            : AppTheme.FollowSystem;
    }

    private static DataSourceType ParseDataSourceType(int value)
    {
        return Enum.IsDefined(typeof(DataSourceType), value)
            ? (DataSourceType)value
            : DataSourceType.Udp;
    }
}

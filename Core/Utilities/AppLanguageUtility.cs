using System.Globalization;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Utilities;

public static class AppLanguageUtility
{
    public static AppLanguage DetectSystemLanguage()
    {
        return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.SimplifiedChinese
            : AppLanguage.English;
    }

    public static AppLanguage Parse(string? value, AppLanguage fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "zh" or "zh-cn" or "zh-hans" => AppLanguage.SimplifiedChinese,
            "en" or "en-us" => AppLanguage.English,
            _ => fallback
        };
    }

    public static string ToStorageValue(this AppLanguage language)
    {
        return language switch
        {
            AppLanguage.SimplifiedChinese => "zh-CN",
            _ => "en-US"
        };
    }

    public static CultureInfo ToCultureInfo(this AppLanguage language)
    {
        return language switch
        {
            AppLanguage.SimplifiedChinese => new CultureInfo("zh-CN"),
            _ => new CultureInfo("en-US")
        };
    }
}

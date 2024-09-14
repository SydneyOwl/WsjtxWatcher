using System.Globalization;

namespace WsjtxWatcher.Utils.UTCTimer;

// TNX FT8CN----
public class UtcTimer
{
    /// <summary>
    ///     获取时间字符串（UTC）。
    /// </summary>
    /// <param name="time">时间戳（毫秒）</param>
    /// <returns>格式化的时间字符串</returns>
    public static string GetTimeStr(long time)
    {
        var curtime = time / 1000;
        var hour = curtime / (60 * 60) % 24; // 小时
        var min = curtime % 3600 / 60; // 分钟
        var sec = curtime % 60; // 秒
        return string.Format("UTC : {0:D2}:{1:D2}:{2:D2}", hour, min, sec);
    }

    /// <summary>
    ///     以HHMMSS格式显示UTC时间。
    /// </summary>
    /// <param name="time">时间戳（毫秒）</param>
    /// <returns>格式化的时间字符串</returns>
    public static string GetTimeHhmmss(long time)
    {
        var curtime = time / 1000;
        var hour = curtime / (60 * 60) % 24; // 小时
        var min = curtime % 3600 / 60; // 分钟
        var sec = curtime % 60; // 秒
        return string.Format("{0:D2}{1:D2}{2:D2}", hour, min, sec);
    }

    /// <summary>
    ///     获取日期字符串（yyyyMMdd格式）。
    /// </summary>
    /// <param name="time">时间戳（毫秒）</param>
    /// <returns>格式化的日期字符串</returns>
    public static string GetYyyymmdd(long time)
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime;
        return dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     获取日期和时间字符串（yyyy-MM-dd HH:mm:ss格式）。
    /// </summary>
    /// <param name="time">时间戳（毫秒）</param>
    /// <returns>格式化的日期时间字符串</returns>
    public static string GetDatetimeStr(long time)
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime;
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     获取日期和时间字符串（yyyyMMdd-HHmmss格式）。
    /// </summary>
    /// <param name="time">时间戳（毫秒）</param>
    /// <returns>格式化的日期时间字符串</returns>
    public static string GetDatetimeYYYYMMDD_HHMMSS(long time)
    {
        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime;
        return dateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    }

    public static string ConvertMillisecondsToTime(long milliseconds)
    {
        // 创建 TimeSpan 对象
        var timeSpan = TimeSpan.FromMilliseconds(milliseconds);

        // 格式化时间字符串
        var formattedTime = string.Format("{0:D2}:{1:D2}:{2:D2}",
            timeSpan.Hours,
            timeSpan.Minutes,
            timeSpan.Seconds);

        return formattedTime;
    }
}
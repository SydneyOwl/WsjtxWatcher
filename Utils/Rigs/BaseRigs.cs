using System.Text;


// TNX FT8CN----
public static class BaseRigOperation
{
    /// <summary>
    ///     获取频率字符串。
    /// </summary>
    /// <param name="freq">频率</param>
    /// <returns>格式化的频率字符串</returns>
    public static string GetFrequencyStr(long freq)
    {
        return string.Format("{0}.{1:D03}MHz", freq / 1000000, freq % 1000000 / 1000);
    }

    /// <summary>
    ///     检查是否在 WSPR2 的频段内。
    /// </summary>
    /// <param name="freq">频率</param>
    /// <returns>是否在频段内</returns>
    public static bool CheckIsWspr2(long freq)
    {
        return (freq >= 137400 && freq <= 137600) // 2190m
               || (freq >= 475400 && freq <= 475600) // 630m
               || (freq >= 1838000 && freq <= 1838200) // 160m
               || (freq >= 3594000 && freq <= 3594200) // 80m
               || (freq >= 5288600 && freq <= 5288800) // 60m
               || (freq >= 7040000 && freq <= 7040200) // 40m
               || (freq >= 10140100 && freq <= 10140300) // 30m
               || (freq >= 14097000 && freq <= 14097200) // 20m
               || (freq >= 18106000 && freq <= 18106200) // 17m
               || (freq >= 21096000 && freq <= 21096200) // 15m
               || (freq >= 24926000 && freq <= 24926200) // 12m
               || (freq >= 28126000 && freq <= 28126200) // 10m
               || (freq >= 50294400 && freq <= 50294600) // 6m
               || (freq >= 70092400 && freq <= 70092600) // 4m
               || (freq >= 144489900 && freq <= 144490100) // 2m
               || (freq >= 432301600 && freq <= 432301800) // 70cm
               || (freq >= 1296501400 && freq <= 1296501600); // 23cm
    }

    /// <summary>
    ///     通过频率获取波长。
    /// </summary>
    /// <param name="freq">频率</param>
    /// <returns>波长字符串</returns>
    public static string GetMeterFromFreq(long freq)
    {
        if (freq >= 135700 && freq <= 137800)
            return "2200m";
        // 2200m
        if (freq >= 472000 && freq <= 479000)
            return "630m";
        // 630m
        if (freq >= 1800000 && freq <= 2000000)
            return "160m";
        // 160m
        if (freq >= 3500000 && freq <= 4000000)
            return "80m";
        // 80m
        if (freq >= 5351500 && freq <= 5366500)
            return "60m";
        // 60m
        if (freq >= 7000000 && freq <= 7300000)
            return "40m";
        // 40m
        if (freq >= 10100000 && freq <= 10150000)
            return "30m";
        // 30m
        if (freq >= 14000000 && freq <= 14350000)
            return "20m";
        // 20m
        if (freq >= 18068000 && freq <= 18168000)
            return "17m";
        // 17m
        if (freq >= 21000000 && freq <= 21450000)
            return "15m";
        // 15m
        if (freq >= 24890000 && freq <= 24990000)
            return "12m";
        // 12m
        if (freq >= 28000000 && freq <= 29700000)
            return "10m";
        // 10m
        if (freq >= 50000000 && freq <= 54000000)
            return "6m";
        // 6m
        if (freq >= 144000000 && freq <= 148000000)
            return "2m";
        // 2m
        if (freq >= 220000000 && freq <= 225000000)
            return "1.25m";
        // 1.25m
        if (freq >= 420000000 && freq <= 450000000)
            return "70cm";
        // 70cm
        if (freq >= 902000000 && freq <= 928000000)
            return "33cm";
        // 33cm
        if (freq >= 1240000000 && freq <= 1300000000)
            return "23cm";
        // 23cm
        return CalculationMeterFromFreq(freq); // 不在范围内，就计算一下
    }

    private static string CalculationMeterFromFreq(long freq)
    {
        if (freq == 0) return "";
        var meter = 300000000f / freq;
        if (meter < 1) // 以厘米为单位
            return string.Format("{0}cm", Math.Round(meter * 10) * 10);
        if (meter < 20) // 小于20米，以米为单位
            return string.Format("{0}m", Math.Round(meter));
        // 大于20米，以10米为单位
        return string.Format("{0}m", Math.Round(meter / 10) * 10);
    }

    /// <summary>
    ///     获取频率的所有信息。
    /// </summary>
    /// <param name="freq">频率</param>
    /// <returns>频率和波长的字符串</returns>
    public static string GetFrequencyAllInfo(long freq)
    {
        return $"{GetFrequencyStr(freq)} ({GetMeterFromFreq(freq)})";
    }

    /// <summary>
    ///     获取浮点形式的频率。
    /// </summary>
    /// <param name="freq">频率</param>
    /// <returns>浮点频率字符串</returns>
    public static string GetFrequencyFloat(long freq)
    {
        return string.Format("{0}.{1:D06}", freq / 1000000, freq % 1000000);
    }

    /// <summary>
    ///     将字节数组转换为十六进制字符串。
    /// </summary>
    /// <param name="data">字节数组</param>
    /// <returns>十六进制字符串</returns>
    public static string ByteToStr(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data) sb.AppendFormat("0x{0:X2} ", b);
        return sb.ToString();
    }
}
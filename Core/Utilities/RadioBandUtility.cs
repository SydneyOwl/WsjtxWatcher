namespace WsjtxWatcher.Core.Utilities;

public static class RadioBandUtility
{
    public static string GetBandName(double frequencyHz)
    {
        var roundedFrequency = (long)Math.Round(frequencyHz, MidpointRounding.AwayFromZero);
        return roundedFrequency switch
        {
            >= 135700 and <= 137800 => "2200m",
            >= 472000 and <= 479000 => "630m",
            >= 1800000 and <= 2000000 => "160m",
            >= 3500000 and <= 4000000 => "80m",
            >= 5351500 and <= 5366500 => "60m",
            >= 7000000 and <= 7300000 => "40m",
            >= 10100000 and <= 10150000 => "30m",
            >= 14000000 and <= 14350000 => "20m",
            >= 18068000 and <= 18168000 => "17m",
            >= 21000000 and <= 21450000 => "15m",
            >= 24890000 and <= 24990000 => "12m",
            >= 28000000 and <= 29700000 => "10m",
            >= 50000000 and <= 54000000 => "6m",
            >= 144000000 and <= 148000000 => "2m",
            >= 220000000 and <= 225000000 => "1.25m",
            >= 420000000 and <= 450000000 => "70cm",
            >= 902000000 and <= 928000000 => "33cm",
            >= 1240000000 and <= 1300000000 => "23cm",
            _ => string.Empty
        };
    }
}

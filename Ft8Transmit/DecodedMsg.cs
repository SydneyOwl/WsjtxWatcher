using System.Globalization;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxWatcher.Database;
using WsjtxWatcher.Utils.Maidenhead;
using WsjtxWatcher.Utils.UTCTimer;
using WsjtxWatcher.Variables;

namespace WsjtxWatcher.Ft8Transmit;

public class DecodedMsg
{
    private static string _tag = "DecMsgs";
    public bool New { get; set; }

    public string DecodeTime { get; set; }

    public int Snr { get; set; }

    public double OffsetTimeSeconds { get; set; }

    public uint OffsetFrequencyHz { get; set; }

    public string Mode { get; set; }

    public string Message { get; set; }

    public bool LowConfidence { get; set; }

    public bool OffAir { get; set; }

    public string Receiver { get; set; }

    public string Transmitter { get; set; }

    public string TransmitterLocation { get; set; } = "";

    public string Distance { get; set; } = "";

    public string ToLocationCountryZh { get; set; }

    public string FromLocationCountryZh { get; set; }

    public string ToLocationCountryEn { get; set; }

    public string FromLocationCountryEn { get; set; }

    public int ToLocationCountryId { get; set; }

    public int FromLocationCountryId { get; set; }

    //这里注意，一定不是我发送的信息！
    public static DecodedMsg RawDecodedToDecodedMsg(Decode dec)
    {
        var tmp = new DecodedMsg();
        var deodedTime = UtcTimer.ConvertMillisecondsToTime(dec.Time);
        tmp.New = dec.New;
        tmp.DecodeTime = deodedTime;
        tmp.Snr = dec.Snr;
        tmp.OffsetTimeSeconds = dec.OffsetTimeSeconds;
        tmp.OffsetFrequencyHz = dec.OffsetFrequencyHz;
        tmp.Mode = dec.Mode;
        tmp.Message = dec.Message.TrimEnd();
        tmp.LowConfidence = dec.LowConfidence;
        tmp.OffAir = dec.OffAir;
        tmp.RecordCallsignAndLocation();
        tmp.CalcCallsign();
        tmp.CalcCountry();
        tmp.CalcDistance();
        Console.WriteLine($"Done. The result appears to beeee:{tmp}");
        return tmp;
    }

    // 获取发射者和接收者的呼号
    private void CalcCallsign()
    {
        // 情况1 CQ (AB) CALLSIGN (GRID)
        var data = Message.Split(" ");
        if (Message.StartsWith("CQ"))
        {
            if (data.Length < 2) return;
            //最后一格不是坐标的话就只能是呼号了（对于标准信息）
            if (!MaidenheadGrid.CheckMaidenhead(data.Last()))
                Transmitter = data.Last();
            else
                // 倒数第二个是呼号
                Transmitter = data[^2];
            return;
        }
        // 对于其他情况，比如 CALLSIGN1 CALLSIGN2 GRID 或者CALLSIGN1 CALLSIGN2 R-01（RR73） 或者 CALLSIGN1 CALLSIGN2

        // 如果是三位的或者干脆啥都没有（对于标准信息！！）
        if (data.Length == 3)
        {
            Transmitter = data[^2];
            Receiver = data[^3];
        }

        // 如果是两位的
        if (data.Length == 2)
        {
            Transmitter = data[^1];
            Receiver = data[^2];
        }
    }

    // 只把呼号和对应坐标入库
    private void RecordCallsignAndLocation()
    {
        // 最后一位肯定是梅登海格，或者是呼号的话说明没发坐标
        var data = Message.Split(" ");
        // 少于两位 一定无坐标（标准信息）
        if (data.Length < 3) return;
        // ignore "<...>"
        // 倒数第二位是呼号！
        var tCallsign = data[^2];
        if (tCallsign.Contains("...")) return;
        var targetLoc = "";
        // 最后一位是坐标
        if (MaidenheadGrid.CheckMaidenhead(data.Last()))
        {
            DatabaseHandler.GetInstance(null).AddCallsignGrid(tCallsign, data.Last());
            targetLoc = data.Last();
        }
        else
        {
            // 否则从数据库中找
            targetLoc = DatabaseHandler.GetInstance(null).QueryGrid(tCallsign);
        }

        TransmitterLocation = targetLoc;
    }

    private void CalcDistance()
    {
        if (string.IsNullOrEmpty(SettingsVariables.MyLocation)) return;
        // 校验我的位置
        if (!MaidenheadGrid.CheckMaidenhead(SettingsVariables.MyLocation)) return;
        double distance = 0;
        if (string.IsNullOrEmpty(TransmitterLocation))
        {
            // 直接用国家计算
            if (!string.IsNullOrEmpty(FromLocationCountryEn))
            {
                var countryDetail = DatabaseHandler.GetInstance(null).QueryCountryByName(FromLocationCountryEn);
                var lat = countryDetail.Latitude;
                var lon = countryDetail.Longitude;
                var countryLatLon = new LatLng(lat, lon);
                var myLatLon = MaidenheadGrid.GridToLatLng(SettingsVariables.MyLocation);
                distance = MaidenheadGrid.GetDist(countryLatLon, myLatLon);
            }
        }
        else
        {
            distance = MaidenheadGrid.GetDist(SettingsVariables.MyLocation, TransmitterLocation);
        }

        if (distance == 0)
            Distance = "? km";
        else
            Distance = Math.Floor(distance).ToString(CultureInfo.InvariantCulture) + " km";
    }

    // 计算所在国家
    private void CalcCountry()
    {
        if (!string.IsNullOrEmpty(Transmitter))
        {
            var result = DatabaseHandler.GetInstance(null).QueryCountryByCallsign(Transmitter);
            FromLocationCountryZh = result.CountryNameCn;
            FromLocationCountryEn = result.CountryNameEn;
            FromLocationCountryId = result.Id;
        }

        if (!string.IsNullOrEmpty(Receiver))
        {
            var result = DatabaseHandler.GetInstance(null).QueryCountryByCallsign(Receiver);
            ToLocationCountryZh = result.CountryNameCn;
            ToLocationCountryEn = result.CountryNameEn;
            ToLocationCountryId = result.Id;
        }
    }

    public override string ToString()
    {
        return
            $"{nameof(New)}: {New}, {nameof(DecodeTime)}: {DecodeTime}, {nameof(Snr)}: {Snr}, {nameof(OffsetTimeSeconds)}: {OffsetTimeSeconds}, {nameof(OffsetFrequencyHz)}: {OffsetFrequencyHz}, {nameof(Mode)}: {Mode}, {nameof(Message)}: {Message}, {nameof(LowConfidence)}: {LowConfidence}, {nameof(OffAir)}: {OffAir}, {nameof(Receiver)}: {Receiver}, {nameof(Transmitter)}: {Transmitter}, {nameof(Distance)}: {Distance}, {nameof(ToLocationCountryZh)}: {ToLocationCountryZh}, {nameof(FromLocationCountryZh)}: {FromLocationCountryZh}, {nameof(ToLocationCountryEn)}: {ToLocationCountryEn}, {nameof(FromLocationCountryEn)}: {FromLocationCountryEn}";
    }
}

// {
//     "New": true,
//     "Time": 25635000,
//     "Snr": -1,
//     "OffsetTimeSeconds": 0.6,
//     "OffsetFrequencyHz": 778,
//     "Mode": "~",
//     "Message": "CQ BG5VLI OL94",
//     "LowConfidence": false,
//     "OffAir": false,
//     "MagicNumber": 2914831322,
//     "SchemaVersion": 2,
//     "MessageType": 2,
//     "Id": "JTDX"
// }
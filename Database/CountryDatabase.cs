using SQLite;

namespace WsjtxWatcher.Database;

[Table("countries")]
public class CountryDatabase
{
    public CountryDatabase(string s)
    {
        var info = s.Split(":");
        if (info.Length < 9) return;
        CountryNameEn = info[0].Replace("\n", "").Trim();
        CqZone = int.Parse(info[1].Replace("\n", "").Replace(" ", ""));
        ItuZone = int.Parse(info[2].Replace("\n", "").Replace(" ", ""));
        Continent = info[3].Replace("\n", "").Replace(" ", "");
        Latitude = float.Parse(info[4].Replace("\n", "").Replace(" ", ""));
        Longitude = float.Parse(info[5].Replace("\n", "").Replace(" ", "")) * -1;
        GmtOffset = float.Parse(info[6].Replace("\n", "").Replace(" ", ""));
        Dxcc = info[7].Replace("\n", "").Replace(" ", "");
    }

    public CountryDatabase()
    {
    }

    [PrimaryKey]
    // [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("country_en")] public string CountryNameEn { get; set; } //国家---

    [Column("country_cn")] public string CountryNameCn { get; set; } //国家中文名---

    [Column("cq_zone")] public int CqZone { get; set; } //CQ分区---

    [Column("itu_zone")] public int ItuZone { get; set; } //ITU分区---

    [Column("continent")] public string Continent { get; set; } //大陆缩写---

    [Column("latitude")] public float Latitude { get; set; } //以度为单位的纬度，+ 表示北---

    [Column("longitude")] public float Longitude { get; set; } //以度为单位的经度，+ 表示西---

    [Column("gmt_offset")] public float GmtOffset { get; set; } //与 GMT 的本地时间偏移---

    [Column("dxcc")] public string Dxcc { get; set; } //DXCC前缀---

    [Ignore] public bool Checked { get; set; } // 是不是我想要的dxcc，这个字段不存在数据库里面

    public override string ToString()
    {
        return
            $"{nameof(Id)}: {Id}, {nameof(CountryNameEn)}: {CountryNameEn}, {nameof(CountryNameCn)}: {CountryNameCn}, {nameof(CqZone)}: {CqZone}, {nameof(ItuZone)}: {ItuZone}, {nameof(Continent)}: {Continent}, {nameof(Latitude)}: {Latitude}, {nameof(Longitude)}: {Longitude}, {nameof(GmtOffset)}: {GmtOffset}, {nameof(Dxcc)}: {Dxcc}";
    }
}
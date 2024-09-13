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
        CQZone = int.Parse(info[1].Replace("\n", "").Replace(" ", ""));
        ITUZone = int.Parse(info[2].Replace("\n", "").Replace(" ", ""));
        Continent = info[3].Replace("\n", "").Replace(" ", "");
        Latitude = float.Parse(info[4].Replace("\n", "").Replace(" ", ""));
        Longitude = float.Parse(info[5].Replace("\n", "").Replace(" ", ""))*-1;
        GMT_offset = float.Parse(info[6].Replace("\n", "").Replace(" ", ""));
        DXCC = info[7].Replace("\n", "").Replace(" ", "");
    }

    public CountryDatabase()
    {
    }

    [PrimaryKey]
    // [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("country_en")] public string CountryNameEn { get; set; } //国家---

    [Column("country_cn")] public string CountryNameCN { get; set; } //国家中文名---

    [Column("cq_zone")] public int CQZone { get; set; } //CQ分区---

    [Column("itu_zone")] public int ITUZone { get; set; } //ITU分区---

    [Column("continent")] public string Continent { get; set; } //大陆缩写---

    [Column("latitude")] public float Latitude { get; set; } //以度为单位的纬度，+ 表示北---

    [Column("longitude")] public float Longitude { get; set; } //以度为单位的经度，+ 表示西---

    [Column("gmt_offset")] public float GMT_offset { get; set; } //与 GMT 的本地时间偏移---

    [Column("dxcc")] public string DXCC { get; set; } //DXCC前缀---

    public override string ToString()
    {
        return
            $"{nameof(Id)}: {Id}, {nameof(CountryNameEn)}: {CountryNameEn}, {nameof(CountryNameCN)}: {CountryNameCN}, {nameof(CQZone)}: {CQZone}, {nameof(ITUZone)}: {ITUZone}, {nameof(Continent)}: {Continent}, {nameof(Latitude)}: {Latitude}, {nameof(Longitude)}: {Longitude}, {nameof(GMT_offset)}: {GMT_offset}, {nameof(DXCC)}: {DXCC}";
    }
}
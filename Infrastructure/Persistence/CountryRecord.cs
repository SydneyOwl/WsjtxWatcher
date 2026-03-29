using SQLite;

namespace WsjtxWatcher.Infrastructure.Persistence;

[Table("countries")]
internal sealed class CountryRecord
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Indexed]
    [Column("english_name")]
    public string EnglishName { get; set; } = string.Empty;

    [Column("dxcc_prefix")]
    public string DxccPrefix { get; set; } = string.Empty;

    [Column("cq_zone")]
    public int CqZone { get; set; }

    [Column("itu_zone")]
    public int ItuZone { get; set; }

    [Column("continent")]
    public string Continent { get; set; } = string.Empty;

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("gmt_offset")]
    public double GmtOffset { get; set; }
}

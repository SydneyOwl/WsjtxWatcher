using SQLite;

namespace WsjtxWatcher.Database;

[Table("callsigns")]
public class CallsignDatabase
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("country_id")] public int CountryId { get; set; }

    [Column("callsign")] public string Callsign { get; set; }
}
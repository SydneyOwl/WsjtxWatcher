using SQLite;

namespace WsjtxWatcher.Infrastructure.Persistence;

[Table("callsigns")]
internal sealed class CallsignPrefixRecord
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Indexed]
    [Column("country_id")]
    public int CountryId { get; set; }

    [Indexed]
    [Column("callsign")]
    public string Callsign { get; set; } = string.Empty;
}

using SQLite;

namespace WsjtxWatcher.Infrastructure.Persistence;

[Table("ignored_callsigns")]
internal sealed class IgnoredCallsignRecord
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    [Column("callsign")]
    public string Callsign { get; set; } = string.Empty;

    [Indexed]
    [Column("band")]
    public string Band { get; set; } = string.Empty;
}

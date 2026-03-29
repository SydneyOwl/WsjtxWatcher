using SQLite;

namespace WsjtxWatcher.Infrastructure.Persistence;

[Table("grid_cache")]
internal sealed class GridCacheRecord
{
    [PrimaryKey]
    [Column("callsign")]
    public string Callsign { get; set; } = string.Empty;

    [Column("grid_square")]
    public string GridSquare { get; set; } = string.Empty;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; }
}

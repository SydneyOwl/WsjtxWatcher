using SQLite;

namespace WsjtxWatcher.Database;

[Table("callsign_grid")]
public class CallsignGridDatabase
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

   
    [Unique] [Column("callsign")] public string Callsign { get; set; }

    [Column("grid")] public string Grid { get; set; }
}
using SQLite;

namespace WsjtxWatcher.Infrastructure.Persistence;

[Table("db_metadata")]
internal sealed class DbMetadataRecord
{
    [PrimaryKey]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string Value { get; set; } = string.Empty;
}

using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Infrastructure.Persistence;

public sealed class SqliteIgnoredCallsignStore : IIgnoredCallsignStore
{
    private readonly WatcherDatabaseService _databaseService;

    public SqliteIgnoredCallsignStore(WatcherDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<IReadOnlyList<IgnoredCallsignEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var records = await connection.Table<IgnoredCallsignRecord>()
            .OrderBy(record => record.Band)
            .ThenBy(record => record.Callsign)
            .ToListAsync()
            .ConfigureAwait(false);

        return IgnoredCallsignMatcher.NormalizeEntries(records.Select(record => new IgnoredCallsignEntry
        {
            Callsign = record.Callsign,
            Band = record.Band
        }));
    }

    public async Task SaveAsync(IEnumerable<IgnoredCallsignEntry> entries, CancellationToken cancellationToken = default)
    {
        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var normalizedEntries = IgnoredCallsignMatcher.NormalizeEntries(entries);
        var records = normalizedEntries.Select(entry => new IgnoredCallsignRecord
        {
            Id = CreateKey(entry.Callsign, entry.Band),
            Callsign = entry.Callsign,
            Band = entry.Band
        }).ToList();

        await connection.RunInTransactionAsync(db =>
        {
            db.DeleteAll<IgnoredCallsignRecord>();
            if (records.Count > 0)
            {
                db.InsertAll(records);
            }
        }).ConfigureAwait(false);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.DeleteAllAsync<IgnoredCallsignRecord>().ConfigureAwait(false);
    }

    private static string CreateKey(string callsign, string band)
    {
        return $"{band.ToUpperInvariant()}|{callsign.ToUpperInvariant()}";
    }
}

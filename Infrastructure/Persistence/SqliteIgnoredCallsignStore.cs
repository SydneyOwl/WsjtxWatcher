using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Infrastructure.Persistence;

public sealed class SqliteIgnoredCallsignStore : IIgnoredCallsignStore
{
    private const int MaxParametersPerQuery = 900;
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

        return records
            .Select(record => new IgnoredCallsignEntry
            {
                Callsign = record.Callsign,
                Band = record.Band
            })
            .ToArray();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>("select count(*) from ignored_callsigns").ConfigureAwait(false);
    }

    public async Task<bool> AddAsync(IgnoredCallsignEntry entry, CancellationToken cancellationToken = default)
    {
        var record = CreateRecord(entry);
        if (record is null)
        {
            return false;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var affectedRows = await connection.ExecuteAsync(
            "insert or ignore into ignored_callsigns (id, callsign, band) values (?, ?, ?)",
            record.Id,
            record.Callsign,
            record.Band).ConfigureAwait(false);
        return affectedRows > 0;
    }

    public async Task<IgnoredCallsignMergeResult> MergeAsync(
        IEnumerable<IgnoredCallsignEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var normalizedEntries = IgnoredCallsignMatcher.NormalizeEntries(entries);
        if (normalizedEntries.Count == 0)
        {
            return new IgnoredCallsignMergeResult();
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var addedCount = 0;
        await connection.RunInTransactionAsync(db =>
        {
            foreach (var entry in normalizedEntries)
            {
                var record = CreateRecord(entry);
                if (record is null)
                {
                    continue;
                }

                addedCount += db.Execute(
                    "insert or ignore into ignored_callsigns (id, callsign, band) values (?, ?, ?)",
                    record.Id,
                    record.Callsign,
                    record.Band);
            }
        }).ConfigureAwait(false);

        return new IgnoredCallsignMergeResult
        {
            CandidateCount = normalizedEntries.Count,
            AddedCount = addedCount,
            DuplicateCount = normalizedEntries.Count - addedCount
        };
    }

    public async Task<bool> RemoveAsync(IgnoredCallsignEntry entry, CancellationToken cancellationToken = default)
    {
        var key = IgnoredCallsignMatcher.CreateLookupKey(entry.Callsign, entry.Band);
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteAsync("delete from ignored_callsigns where id = ?", key).ConfigureAwait(false) > 0;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.DeleteAllAsync<IgnoredCallsignRecord>().ConfigureAwait(false);
    }

    public async Task<IReadOnlySet<string>> FindMatchesAsync(
        IEnumerable<string> lookupKeys,
        CancellationToken cancellationToken = default)
    {
        var normalizedKeys = lookupKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedKeys.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var matches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var batch in normalizedKeys.Chunk(MaxParametersPerQuery))
        {
            var placeholders = string.Join(", ", Enumerable.Repeat("?", batch.Length));
            var sql = $"select id from ignored_callsigns where id in ({placeholders})";
            var rows = await connection
                .QueryAsync<IgnoredCallsignLookupRow>(sql, batch.Cast<object>().ToArray())
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id))
                {
                    matches.Add(row.Id);
                }
            }
        }

        return matches;
    }

    private static IgnoredCallsignRecord? CreateRecord(IgnoredCallsignEntry entry)
    {
        var key = IgnoredCallsignMatcher.CreateLookupKey(entry.Callsign, entry.Band);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return new IgnoredCallsignRecord
        {
            Id = key,
            Callsign = IgnoredCallsignMatcher.NormalizeCallsign(entry.Callsign),
            Band = IgnoredCallsignMatcher.NormalizeBand(entry.Band)
        };
    }

    private sealed class IgnoredCallsignLookupRow
    {
        public string Id { get; init; } = string.Empty;
    }
}

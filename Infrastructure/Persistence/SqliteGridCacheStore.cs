using System.Collections.Concurrent;
using SQLite;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Persistence;

public sealed class SqliteGridCacheStore : IGridCacheStore
{
    private const int MaxCacheEntries = 2048;
    private readonly WatcherDatabaseService _databaseService;
    private readonly ConcurrentDictionary<string, string> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _missingCache = new(StringComparer.OrdinalIgnoreCase);

    public SqliteGridCacheStore(WatcherDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<string?> GetGridAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return null;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var normalizedCallsign = NormalizeCallsign(callsign);
        if (_memoryCache.TryGetValue(normalizedCallsign, out var cachedGrid))
        {
            return cachedGrid;
        }

        if (_missingCache.ContainsKey(normalizedCallsign))
        {
            return null;
        }

        var record = await connection.Table<GridCacheRecord>()
            .Where(item => item.Callsign == normalizedCallsign)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (record is null)
        {
            CacheMissing(normalizedCallsign);
            return null;
        }

        CacheGrid(record.Callsign, record.GridSquare);
        return record.GridSquare;
    }

    public async Task SaveGridAsync(string callsign, string grid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign) || string.IsNullOrWhiteSpace(grid))
        {
            return;
        }

        await SaveManyAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeCallsign(callsign)] = NormalizeGrid(grid)
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveManyAsync(IReadOnlyDictionary<string, string> callsignToGrid, CancellationToken cancellationToken = default)
    {
        if (callsignToGrid.Count == 0)
        {
            return;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var records = callsignToGrid
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => new GridCacheRecord
            {
                Callsign = NormalizeCallsign(entry.Key),
                GridSquare = NormalizeGrid(entry.Value),
                UpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (records.Count == 0)
        {
            return;
        }

        await connection.RunInTransactionAsync(db =>
        {
            foreach (var record in records)
            {
                db.InsertOrReplace(record);
            }
        }).ConfigureAwait(false);

        foreach (var record in records)
        {
            CacheGrid(record.Callsign, record.GridSquare);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.DeleteAllAsync<GridCacheRecord>().ConfigureAwait(false);
        _memoryCache.Clear();
        _missingCache.Clear();
    }

    private void CacheGrid(string callsign, string grid)
    {
        if (_memoryCache.Count >= MaxCacheEntries)
        {
            _memoryCache.Clear();
        }

        _memoryCache[callsign] = grid;
        _missingCache.TryRemove(callsign, out _);
    }

    private void CacheMissing(string callsign)
    {
        if (_missingCache.Count >= MaxCacheEntries)
        {
            _missingCache.Clear();
        }

        _missingCache[callsign] = 0;
    }

    private static string NormalizeCallsign(string callsign)
    {
        return callsign.Trim().ToUpperInvariant();
    }

    private static string NormalizeGrid(string grid)
    {
        return grid.Trim().ToUpperInvariant();
    }
}

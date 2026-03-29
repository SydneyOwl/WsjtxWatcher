using System.Text.Json;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Persistence;

public sealed class JsonGridCacheStore : IGridCacheStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, string>? _cache;

    public JsonGridCacheStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<string?> GetGridAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return null;
        }

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _cache!.TryGetValue(callsign.Trim().ToUpperInvariant(), out var grid) ? grid : null;
    }

    public async Task SaveGridAsync(string callsign, string grid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign) || string.IsNullOrWhiteSpace(grid))
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedCoreAsync(cancellationToken).ConfigureAwait(false);
            _cache![callsign.Trim().ToUpperInvariant()] = grid.Trim().ToUpperInvariant();
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedCoreAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return;
        }

        if (!File.Exists(_filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
        _cache = string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ??
              new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(_cache);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
    }
}

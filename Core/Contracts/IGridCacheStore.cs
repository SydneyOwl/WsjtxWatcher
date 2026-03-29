namespace WsjtxWatcher.Core.Contracts;

public interface IGridCacheStore
{
    Task<string?> GetGridAsync(string callsign, CancellationToken cancellationToken = default);
    Task SaveGridAsync(string callsign, string grid, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}

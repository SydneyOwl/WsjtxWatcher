using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface ICloudlogIgnoredCallsignImportService
{
    Task<CloudlogIgnoredCallsignImportData> DownloadIgnoredCallsignEntriesAsync(
        string baseUrl,
        string username,
        string password,
        string stationId,
        int lookbackDays,
        CancellationToken cancellationToken = default);
}

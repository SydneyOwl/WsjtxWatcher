namespace WsjtxWatcher.Core.Models;

public sealed class CloudlogIgnoredCallsignImportData
{
    public int RecordCount { get; init; }

    public IReadOnlyList<IgnoredCallsignEntry> Entries { get; init; } = [];
}

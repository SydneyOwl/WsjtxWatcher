namespace WsjtxWatcher.Core.Models;

public sealed class IgnoredCallsignMergeResult
{
    public int CandidateCount { get; init; }

    public int AddedCount { get; init; }

    public int DuplicateCount { get; init; }
}

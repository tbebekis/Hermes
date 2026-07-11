// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Stores recent in-memory synchronization activity entries.
/// </summary>
public class SyncActivityStore
{
    // ● fields

    readonly object fLock = new();
    readonly List<SyncActivityRecord> fRecords = new();

    // ● private

    void Add(SyncActivityRecord Record)
    {
        lock (fLock)
        {
            fRecords.Insert(0, Record);

            while (fRecords.Count > MaxCount)
                fRecords.RemoveAt(fRecords.Count - 1);
        }
    }

    // ● public

    /// <summary>
    /// Adds a successful synchronization pass entry.
    /// </summary>
    public void AddSuccess(string SyncRootId, MetadataSyncRunResult Result)
    {
        Guard.NotNull(Result, nameof(Result));

        Add(new SyncActivityRecord()
        {
            TimestampUtc = DateTime.UtcNow,
            Level = Result.PendingExecutionCount == 0 && Result.OpenConflictCount == 0 ? "Information" : "Warning",
            SyncRootId = SyncRootId ?? string.Empty,
            Title = "Sync pass completed",
            Details = "Kind: " + Result.Kind
                + ". Local items: " + Result.LocalObservedItemCount.ToString()
                + ". Remote items: " + Result.RemoteObservedItemCount.ToString()
                + ". Remote changes: " + Result.RemoteObservedChangeCount.ToString()
                + ". Decisions: " + Result.DecisionCount.ToString()
                + ". Pending executions: " + Result.PendingExecutionCount.ToString()
                + ". Open conflicts: " + Result.OpenConflictCount.ToString()
                + ". Pending summary: " + Result.PendingExecutionSummary
                + ". Pending diffs: " + Result.PendingDiffSummary
                + ". Uncommitted summary: " + Result.UncommittedExecutionSummary
                + ". Uncommitted messages: " + Result.UncommittedExecutionMessages + ".",
        });
    }
    /// <summary>
    /// Adds a failed synchronization pass entry.
    /// </summary>
    public void AddFailure(string SyncRootId, string Message)
    {
        Add(new SyncActivityRecord()
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Error",
            SyncRootId = SyncRootId ?? string.Empty,
            Title = "Sync pass failed",
            Details = Message ?? string.Empty,
        });
    }
    /// <summary>
    /// Returns recent activity entries.
    /// </summary>
    public IReadOnlyList<SyncActivityRecord> GetRecent()
    {
        lock (fLock)
            return fRecords.ToList();
    }

    // ● properties

    /// <summary>
    /// Gets or sets the maximum number of activity entries kept in memory.
    /// </summary>
    public int MaxCount { get; set; } = 200;
}

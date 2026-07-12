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
    static string ResolveLevel(MetadataSyncRunResult Result)
    {
        if (Result.UncommittedExecutionCount != 0)
            return "Error";

        if (Result.OpenConflictCount != 0)
            return "Warning";

        return "Information";
    }

    // ● public

    /// <summary>
    /// Adds an informational synchronization activity entry.
    /// </summary>
    public void AddInformation(string SyncRootId, string Title, string Details)
    {
        Add(new SyncActivityRecord()
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Information",
            SyncRootId = SyncRootId ?? string.Empty,
            Title = Title ?? string.Empty,
            Details = Details ?? string.Empty,
        });
    }
    /// <summary>
    /// Adds a warning synchronization activity entry.
    /// </summary>
    public void AddWarning(string SyncRootId, string Title, string Details)
    {
        Add(new SyncActivityRecord()
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Warning",
            SyncRootId = SyncRootId ?? string.Empty,
            Title = Title ?? string.Empty,
            Details = Details ?? string.Empty,
        });
    }
    /// <summary>
    /// Adds a successful synchronization pass entry.
    /// </summary>
    public void AddSuccess(string SyncRootId, MetadataSyncRunResult Result)
    {
        Guard.NotNull(Result, nameof(Result));

        Add(new SyncActivityRecord()
        {
            TimestampUtc = DateTime.UtcNow,
            Level = ResolveLevel(Result),
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
                + ". Committed executions: " + Result.CommittedExecutionCount.ToString()
                + ". Uncommitted executions: " + Result.UncommittedExecutionCount.ToString()
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
    /// <summary>
    /// Clears all activity entries kept in memory.
    /// </summary>
    public void Clear()
    {
        lock (fLock)
            fRecords.Clear();
    }

    // ● properties

    /// <summary>
    /// Gets or sets the maximum number of activity entries kept in memory.
    /// </summary>
    public int MaxCount { get; set; } = 50;
}

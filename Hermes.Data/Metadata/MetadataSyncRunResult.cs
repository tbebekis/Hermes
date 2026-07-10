// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines the remote observation mode used by a metadata synchronization pass.
/// </summary>
public enum MetadataSyncRunKind
{
    /// <summary>
    /// The pass kind is not specified.
    /// </summary>
    None,

    /// <summary>
    /// The pass used a full remote snapshot.
    /// </summary>
    Bootstrap,

    /// <summary>
    /// The pass used incremental remote changes.
    /// </summary>
    Incremental,
}

/// <summary>
/// Contains the result of a metadata synchronization pass that may execute pending requests.
/// </summary>
public class MetadataSyncRunResult
{
    // ● private

    static string FormatPendingExecutionSummary(MetadataSyncSessionResult Result)
    {
        if (Result == null || Result.PendingExecutionRequests.Count == 0)
            return "none";

        return string.Join(
            ", ",
            Result.PendingExecutionRequests
                .Where(Item => Item.Decision != null)
                .GroupBy(Item => Item.Decision.DecisionKind)
                .OrderBy(Group => Group.Key)
                .Select(Group => $"{Group.Key}={Group.Count()}"));
    }
    static string FormatPendingDiffSummary(MetadataSyncSessionResult Result)
    {
        if (Result == null || Result.PendingExecutionRequests.Count == 0)
            return "none";

        return string.Join(
            ", ",
            Result.PendingExecutionRequests
                .Where(Item => Item.Decision != null)
                .GroupBy(Item => Item.Decision.DiffKind)
                .OrderBy(Group => Group.Key)
                .Select(Group => $"{Group.Key}={Group.Count()}"));
    }

    // ● properties

    /// <summary>
    /// Gets or sets the metadata session result.
    /// </summary>
    public MetadataSyncSessionResult SessionResult { get; set; }

    /// <summary>
    /// Gets or sets the execution apply result.
    /// </summary>
    public SyncExecutionApplyResult ExecutionApplyResult { get; set; } = new();

    /// <summary>
    /// Gets or sets the remote observation mode used by the run.
    /// </summary>
    public MetadataSyncRunKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the number of local items observed by the run.
    /// </summary>
    public int LocalObservedItemCount { get; set; }

    /// <summary>
    /// Gets or sets the number of remote items observed by a bootstrap run.
    /// </summary>
    public int RemoteObservedItemCount { get; set; }

    /// <summary>
    /// Gets or sets the number of remote changes observed by an incremental run.
    /// </summary>
    public int RemoteObservedChangeCount { get; set; }

    /// <summary>
    /// Gets the number of planner decisions produced by the run.
    /// </summary>
    public int DecisionCount => SessionResult?.Decisions.Count ?? 0;

    /// <summary>
    /// Gets the number of execution requests left pending by the run.
    /// </summary>
    public int PendingExecutionCount => SessionResult?.PendingExecutionRequests.Count ?? 0;

    /// <summary>
    /// Gets the number of execution results committed by the run.
    /// </summary>
    public int CommittedExecutionCount => ExecutionApplyResult?.CommittedResults.Count ?? 0;

    /// <summary>
    /// Gets the number of execution results not committed by the run.
    /// </summary>
    public int UncommittedExecutionCount => ExecutionApplyResult?.UncommittedResults.Count ?? 0;

    /// <summary>
    /// Gets a summary of pending execution request decision kinds.
    /// </summary>
    public string PendingExecutionSummary => FormatPendingExecutionSummary(SessionResult);

    /// <summary>
    /// Gets a summary of pending execution request diff kinds.
    /// </summary>
    public string PendingDiffSummary => FormatPendingDiffSummary(SessionResult);
}

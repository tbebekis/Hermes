// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the outcome of executing a pending synchronization request.
/// </summary>
public class SyncExecutionResult
{
    // ● properties

    /// <summary>
    /// Gets or sets the request that was executed.
    /// </summary>
    public SyncExecutionRequest Request { get; set; }

    /// <summary>
    /// Gets or sets the execution result kind.
    /// </summary>
    public SyncExecutionResultKind ResultKind { get; set; }

    /// <summary>
    /// Gets or sets the structured storage error when execution failed at the storage provider.
    /// </summary>
    public StorageError Error { get; set; }

    /// <summary>
    /// Gets or sets the remote item observed as the result of a successful mutation.
    /// </summary>
    public StorageItem RemoteItem { get; set; }

    /// <summary>
    /// Gets or sets a diagnostic message for the execution result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

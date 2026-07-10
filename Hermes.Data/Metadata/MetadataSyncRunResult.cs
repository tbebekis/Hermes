// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the result of a metadata synchronization pass that may execute pending requests.
/// </summary>
public class MetadataSyncRunResult
{
    // ● properties

    /// <summary>
    /// Gets or sets the metadata session result.
    /// </summary>
    public MetadataSyncSessionResult SessionResult { get; set; }

    /// <summary>
    /// Gets or sets the execution apply result.
    /// </summary>
    public SyncExecutionApplyResult ExecutionApplyResult { get; set; } = new();
}

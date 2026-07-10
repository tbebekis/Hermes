// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines the outcome of executing a pending synchronization request.
/// </summary>
public enum SyncExecutionResultKind
{
    /// <summary>
    /// The request was executed and verified at both endpoints.
    /// </summary>
    CompletedAndVerified,

    /// <summary>
    /// The request failed and may be retried later.
    /// </summary>
    FailedRetryable,

    /// <summary>
    /// The request failed and should not be retried without a new plan.
    /// </summary>
    FailedPermanent,

    /// <summary>
    /// The request reached a conflict that requires resolution.
    /// </summary>
    Conflict,

    /// <summary>
    /// The request is blocked by an external condition.
    /// </summary>
    Blocked
}

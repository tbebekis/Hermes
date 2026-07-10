// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization execution result creation.
/// </summary>
public class SyncExecutionResultFactoryTests
{
    // ● private

    static SyncExecutionRequest Request() => new()
    {
        Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalChanged, SyncPlanDecisionKind.UploadToRemote),
    };
    static StorageError Error(StorageErrorKind Kind, bool IsRetryable = false)
    {
        return new StorageError(
            Kind,
            "storage error",
            IsRetryable,
            false,
            TimeSpan.Zero,
            "provider",
            string.Empty,
            string.Empty,
            "operation",
            "item-1",
            string.Empty,
            null);
    }

    // ● public

    /// <summary>
    /// Verifies completed execution result creation.
    /// </summary>
    [Fact]
    public void CompletedCreatesVerifiedResult()
    {
        SyncExecutionRequest ExecutionRequest = Request();

        SyncExecutionResult Result = SyncExecutionResultFactory.Completed(ExecutionRequest);

        Assert.Same(ExecutionRequest, Result.Request);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Result.ResultKind);
    }

    /// <summary>
    /// Verifies blocked execution result creation.
    /// </summary>
    [Fact]
    public void BlockedCreatesBlockedResult()
    {
        SyncExecutionRequest ExecutionRequest = Request();

        SyncExecutionResult Result = SyncExecutionResultFactory.Blocked(ExecutionRequest, "blocked");

        Assert.Same(ExecutionRequest, Result.Request);
        Assert.Equal(SyncExecutionResultKind.Blocked, Result.ResultKind);
        Assert.Equal("blocked", Result.Message);
    }

    /// <summary>
    /// Verifies retryable storage errors create retryable execution results.
    /// </summary>
    [Fact]
    public void FromStorageErrorMapsRetryableErrors()
    {
        SyncExecutionResult Result = SyncExecutionResultFactory.FromStorageError(Request(), Error(StorageErrorKind.Unknown, true));

        Assert.Equal(SyncExecutionResultKind.FailedRetryable, Result.ResultKind);
        Assert.Equal("storage error", Result.Message);
    }

    /// <summary>
    /// Verifies storage conflicts create conflict execution results.
    /// </summary>
    [Fact]
    public void FromStorageErrorMapsConflict()
    {
        SyncExecutionResult Result = SyncExecutionResultFactory.FromStorageError(Request(), Error(StorageErrorKind.Conflict));

        Assert.Equal(SyncExecutionResultKind.Conflict, Result.ResultKind);
    }

    /// <summary>
    /// Verifies permission errors create blocked execution results.
    /// </summary>
    [Fact]
    public void FromStorageErrorMapsPermissionDeniedAsBlocked()
    {
        SyncExecutionResult Result = SyncExecutionResultFactory.FromStorageError(Request(), Error(StorageErrorKind.PermissionDenied));

        Assert.Equal(SyncExecutionResultKind.Blocked, Result.ResultKind);
    }

    /// <summary>
    /// Verifies invalid requests create permanent failure execution results.
    /// </summary>
    [Fact]
    public void FromStorageErrorMapsInvalidRequestAsPermanent()
    {
        SyncExecutionResult Result = SyncExecutionResultFactory.FromStorageError(Request(), Error(StorageErrorKind.InvalidRequest));

        Assert.Equal(SyncExecutionResultKind.FailedPermanent, Result.ResultKind);
    }
}

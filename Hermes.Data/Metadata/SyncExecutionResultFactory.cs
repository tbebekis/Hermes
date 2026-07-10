// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Creates synchronization execution results from executor outcomes.
/// </summary>
static public class SyncExecutionResultFactory
{
    // ● private

    static SyncExecutionResultKind ResultKind(StorageError Error)
    {
        Guard.NotNull(Error, nameof(Error));

        if (Error.IsRetryable)
            return SyncExecutionResultKind.FailedRetryable;

        return Error.Kind switch
        {
            StorageErrorKind.Conflict => SyncExecutionResultKind.Conflict,
            StorageErrorKind.RateLimited => SyncExecutionResultKind.FailedRetryable,
            StorageErrorKind.TemporarilyUnavailable => SyncExecutionResultKind.FailedRetryable,
            StorageErrorKind.PermissionDenied => SyncExecutionResultKind.Blocked,
            StorageErrorKind.CheckpointInvalid => SyncExecutionResultKind.Blocked,
            StorageErrorKind.NotFound => SyncExecutionResultKind.FailedPermanent,
            StorageErrorKind.InvalidRequest => SyncExecutionResultKind.FailedPermanent,
            _ => SyncExecutionResultKind.FailedPermanent,
        };
    }

    // ● public

    /// <summary>
    /// Creates a completed and verified execution result.
    /// </summary>
    static public SyncExecutionResult Completed(SyncExecutionRequest Request)
    {
        Guard.NotNull(Request, nameof(Request));

        return new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = SyncExecutionResultKind.CompletedAndVerified,
        };
    }
    /// <summary>
    /// Creates a completed and verified execution result with the affected local relative path.
    /// </summary>
    static public SyncExecutionResult Completed(SyncExecutionRequest Request, string LocalRelativePath)
    {
        Guard.NotNull(Request, nameof(Request));

        return new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = SyncExecutionResultKind.CompletedAndVerified,
            LocalRelativePath = LocalRelativePath ?? string.Empty,
        };
    }
    /// <summary>
    /// Creates a completed and verified execution result with the resulting remote item.
    /// </summary>
    static public SyncExecutionResult Completed(SyncExecutionRequest Request, StorageItem RemoteItem)
    {
        Guard.NotNull(Request, nameof(Request));
        Guard.NotNull(RemoteItem, nameof(RemoteItem));

        return new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = SyncExecutionResultKind.CompletedAndVerified,
            RemoteItem = RemoteItem,
        };
    }
    /// <summary>
    /// Creates a completed and verified execution result with the resulting remote item and affected local relative path.
    /// </summary>
    static public SyncExecutionResult Completed(SyncExecutionRequest Request, StorageItem RemoteItem, string LocalRelativePath)
    {
        Guard.NotNull(Request, nameof(Request));
        Guard.NotNull(RemoteItem, nameof(RemoteItem));

        return new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = SyncExecutionResultKind.CompletedAndVerified,
            RemoteItem = RemoteItem,
            LocalRelativePath = LocalRelativePath ?? string.Empty,
        };
    }

    /// <summary>
    /// Creates a blocked execution result.
    /// </summary>
    static public SyncExecutionResult Blocked(SyncExecutionRequest Request, string Message)
    {
        Guard.NotNull(Request, nameof(Request));

        return new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = SyncExecutionResultKind.Blocked,
            Message = Message ?? string.Empty,
        };
    }

    /// <summary>
    /// Creates an execution result from a structured storage error.
    /// </summary>
    static public SyncExecutionResult FromStorageError(SyncExecutionRequest Request, StorageError Error)
    {
        Guard.NotNull(Request, nameof(Request));
        Guard.NotNull(Error, nameof(Error));

        return new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = ResultKind(Error),
            Error = Error,
            Message = Error.Message,
        };
    }
}

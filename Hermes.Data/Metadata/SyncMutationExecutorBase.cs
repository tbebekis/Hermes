// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Base class for executors that mutate local and remote synchronization endpoints.
/// </summary>
public class SyncMutationExecutorBase : SyncExecutorBase
{
    // ● fields

    readonly ILocalSyncMutationEndpoint fLocalEndpoint;
    readonly IRemoteSyncMutationEndpoint fRemoteEndpoint;

    // ● protected

    /// <summary>
    /// Executes an upload to remote storage.
    /// </summary>
    protected virtual async Task<SyncExecutionResult> ExecuteUploadToRemoteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        StorageResult<StorageItem> Result;

        if (string.Equals(Intent.ItemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            Result = await RemoteEndpoint.CreateFolderAsync(Intent.Name, Intent.RemoteParentId, CancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(Intent.RemoteItemId))
        {
            Result = await RemoteEndpoint.UpdateFileContentAsync(
                Intent.RemoteItemId,
                LocalEndpoint.ResolvePath(Intent.LocalRelativePath),
                CancellationToken);
        }
        else
        {
            Result = await RemoteEndpoint.UploadFileAsync(
                LocalEndpoint.ResolvePath(Intent.LocalRelativePath),
                Intent.RemoteParentId,
                CancellationToken);
        }

        if (Result.Succeeded)
            return SyncExecutionResultFactory.Completed(Intent.Request, Result.Value, Intent.LocalRelativePath);

        return SyncExecutionResultFactory.FromStorageError(Intent.Request, Result.Error);
    }

    /// <summary>
    /// Executes a download to the local filesystem.
    /// </summary>
    protected virtual async Task<SyncExecutionResult> ExecuteDownloadToLocalAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        if (string.Equals(Intent.ItemType, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            Result DirectoryResult = await LocalEndpoint.CreateDirectoryAsync(Intent.LocalRelativePath, CancellationToken);

            if (DirectoryResult.Succeeded)
                return SyncExecutionResultFactory.Completed(Intent.Request, Intent.LocalRelativePath);

            return new SyncExecutionResult()
            {
                Request = Intent.Request,
                ResultKind = SyncExecutionResultKind.FailedPermanent,
                Message = DirectoryResult.ErrorText,
            };
        }

        Result ParentResult = await LocalEndpoint.EnsureParentDirectoryAsync(Intent.LocalRelativePath, CancellationToken);

        if (ParentResult.Failed)
        {
            return new SyncExecutionResult()
            {
                Request = Intent.Request,
                ResultKind = SyncExecutionResultKind.FailedPermanent,
                Message = ParentResult.ErrorText,
            };
        }

        StorageResult<StorageItem> Result = await RemoteEndpoint.DownloadFileAsync(
            Intent.RemoteItemId,
            LocalEndpoint.ResolvePath(Intent.LocalRelativePath),
            CancellationToken);

        if (Result.Succeeded)
            return SyncExecutionResultFactory.Completed(Intent.Request, Result.Value, Intent.LocalRelativePath);

        return SyncExecutionResultFactory.FromStorageError(Intent.Request, Result.Error);
    }

    /// <summary>
    /// Executes a remote namespace change against the local filesystem.
    /// </summary>
    protected virtual async Task<SyncExecutionResult> ExecuteApplyRemoteNamespaceToLocalAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        Result Result = string.Equals(Intent.ItemType, "Folder", StringComparison.OrdinalIgnoreCase)
            ? await LocalEndpoint.MoveDirectoryAsync(Intent.SourceLocalRelativePath, Intent.LocalRelativePath, CancellationToken)
            : await LocalEndpoint.MoveFileAsync(Intent.SourceLocalRelativePath, Intent.LocalRelativePath, CancellationToken);

        if (Result.Succeeded)
            return SyncExecutionResultFactory.Completed(Intent.Request, Intent.LocalRelativePath);

        return new SyncExecutionResult()
        {
            Request = Intent.Request,
            ResultKind = SyncExecutionResultKind.FailedPermanent,
            Message = Result.ErrorText,
        };
    }

    /// <summary>
    /// Executes local delete propagation to remote storage.
    /// </summary>
    protected virtual async Task<SyncExecutionResult> ExecutePropagateLocalDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        StorageResult<StorageItem> Result = await RemoteEndpoint.DeleteItemAsync(Intent.RemoteItemId, CancellationToken);

        if (Result.Succeeded)
            return SyncExecutionResultFactory.Completed(Intent.Request, Result.Value);

        return SyncExecutionResultFactory.FromStorageError(Intent.Request, Result.Error);
    }

    /// <summary>
    /// Executes remote delete propagation to the local filesystem.
    /// </summary>
    protected virtual async Task<SyncExecutionResult> ExecutePropagateRemoteDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        Result Result = await LocalEndpoint.DeleteItemAsync(Intent.LocalRelativePath, CancellationToken);

        if (Result.Succeeded)
            return SyncExecutionResultFactory.Completed(Intent.Request);

        return new SyncExecutionResult()
        {
            Request = Intent.Request,
            ResultKind = SyncExecutionResultKind.FailedPermanent,
            Message = Result.ErrorText,
        };
    }

    /// <summary>
    /// Executes an intent that passed common validation.
    /// </summary>
    protected override Task<SyncExecutionResult> ExecuteIntentAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        Guard.NotNull(Intent, nameof(Intent));

        return Intent.IntentKind switch
        {
            SyncExecutionIntentKind.UploadToRemote => ExecuteUploadToRemoteAsync(Intent, CancellationToken),
            SyncExecutionIntentKind.DownloadToLocal => ExecuteDownloadToLocalAsync(Intent, CancellationToken),
            SyncExecutionIntentKind.ApplyRemoteNamespaceToLocal => ExecuteApplyRemoteNamespaceToLocalAsync(Intent, CancellationToken),
            SyncExecutionIntentKind.PropagateLocalDelete => ExecutePropagateLocalDeleteAsync(Intent, CancellationToken),
            SyncExecutionIntentKind.PropagateRemoteDelete => ExecutePropagateRemoteDeleteAsync(Intent, CancellationToken),
            _ => Task.FromResult(SyncExecutionResultFactory.Blocked(Intent.Request, "Execution intent is not supported.")),
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncMutationExecutorBase"/> class.
    /// </summary>
    public SyncMutationExecutorBase(ILocalSyncMutationEndpoint LocalEndpoint, IRemoteSyncMutationEndpoint RemoteEndpoint)
    {
        fLocalEndpoint = Guard.NotNull(LocalEndpoint, nameof(LocalEndpoint));
        fRemoteEndpoint = Guard.NotNull(RemoteEndpoint, nameof(RemoteEndpoint));
    }

    // ● properties

    /// <summary>
    /// Gets the local mutation endpoint.
    /// </summary>
    protected ILocalSyncMutationEndpoint LocalEndpoint => fLocalEndpoint;

    /// <summary>
    /// Gets the remote mutation endpoint.
    /// </summary>
    protected IRemoteSyncMutationEndpoint RemoteEndpoint => fRemoteEndpoint;
}

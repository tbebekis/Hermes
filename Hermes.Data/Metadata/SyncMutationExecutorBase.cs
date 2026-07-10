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
    protected virtual Task<SyncExecutionResult> ExecuteUploadToRemoteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        return Task.FromResult(SyncExecutionResultFactory.Blocked(Intent.Request, "Upload execution is not implemented."));
    }

    /// <summary>
    /// Executes a download to the local filesystem.
    /// </summary>
    protected virtual Task<SyncExecutionResult> ExecuteDownloadToLocalAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        return Task.FromResult(SyncExecutionResultFactory.Blocked(Intent.Request, "Download execution is not implemented."));
    }

    /// <summary>
    /// Executes local delete propagation to remote storage.
    /// </summary>
    protected virtual Task<SyncExecutionResult> ExecutePropagateLocalDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        return Task.FromResult(SyncExecutionResultFactory.Blocked(Intent.Request, "Local delete propagation is not implemented."));
    }

    /// <summary>
    /// Executes remote delete propagation to the local filesystem.
    /// </summary>
    protected virtual Task<SyncExecutionResult> ExecutePropagateRemoteDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        return Task.FromResult(SyncExecutionResultFactory.Blocked(Intent.Request, "Remote delete propagation is not implemented."));
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

// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Runs one metadata synchronization pass for a sync root.
/// </summary>
public class MetadataSyncRunner : IMetadataSyncRunner
{
    // ● fields

    readonly SqlMetadataStore fStore;
    readonly MetadataSyncSession fSession;
    readonly LocalScanner fLocalScanner;
    readonly IStorageProvider fStorageProvider;
    readonly ISyncExecutor fExecutor;

    // ● private

    static RemoteCheckpointRecord CreateCheckpoint(SyncRootRecord Root, string StartPageToken, DateTime UpdatedTime) => new()
    {
        SyncRootId = Root.Id,
        ProviderName = Root.ProviderName,
        ConnectionId = Root.ConnectionId,
        StartPageToken = StartPageToken,
        UpdatedTime = UpdatedTime,
    };
    async Task<Result<List<StorageItem>>> ListRemoteTreeAsync(string RemoteRootItemId, CancellationToken CancellationToken)
    {
        List<StorageItem> Result = new();
        Queue<string> PendingFolderIds = new();
        PendingFolderIds.Enqueue(RemoteRootItemId);

        while (PendingFolderIds.Count != 0)
        {
            string FolderId = PendingFolderIds.Dequeue();
            StorageResult<IReadOnlyList<StorageItem>> ListResult = await fStorageProvider.ListFolderAsync(FolderId, CancellationToken);

            if (ListResult.Failed)
                return Result<List<StorageItem>>.Failure(ListResult.ErrorText);

            foreach (StorageItem Item in ListResult.Value)
            {
                Result.Add(Item);

                if (Item.IsFolder)
                    PendingFolderIds.Enqueue(Item.Id);
            }
        }

        return Result<List<StorageItem>>.Success(Result);
    }
    async Task<Result<MetadataSyncRunResult>> RunBootstrapAsync(
        SyncRootRecord Root,
        IReadOnlyList<LocalScanItem> LocalItems,
        DateTime LocalObservedTime,
        DateTime RemoteObservedTime,
        DateTime CommittedTime,
        string ScanId,
        CancellationToken CancellationToken)
    {
        StorageResult<string> TokenResult = await fStorageProvider.GetStartPageTokenAsync(CancellationToken);

        if (TokenResult.Failed)
            return Result<MetadataSyncRunResult>.Failure(TokenResult.ErrorText);

        Result<List<StorageItem>> RemoteItemsResult = await ListRemoteTreeAsync(Root.RemoteRootItemId, CancellationToken);

        if (RemoteItemsResult.Failed)
            return Result<MetadataSyncRunResult>.Failure(RemoteItemsResult.ErrorText);

        RemoteCheckpointRecord Checkpoint = CreateCheckpoint(Root, TokenResult.Value, RemoteObservedTime);
        MetadataSyncRunResult RunResult = await fSession.AdvanceWithRemoteSnapshotAndExecuteAsync(
            Root.Id,
            LocalItems,
            RemoteItemsResult.Value,
            Checkpoint,
            LocalObservedTime,
            RemoteObservedTime,
            CommittedTime,
            ScanId,
            fExecutor,
            CancellationToken);
        RunResult.Kind = MetadataSyncRunKind.Bootstrap;
        RunResult.LocalObservedItemCount = LocalItems.Count;
        RunResult.RemoteObservedItemCount = RemoteItemsResult.Value.Count;

        return Result<MetadataSyncRunResult>.Success(RunResult);
    }
    async Task<Result<MetadataSyncRunResult>> RunIncrementalAsync(
        SyncRootRecord Root,
        RemoteCheckpointRecord CurrentCheckpoint,
        IReadOnlyList<LocalScanItem> LocalItems,
        DateTime LocalObservedTime,
        DateTime RemoteObservedTime,
        DateTime CommittedTime,
        string ScanId,
        CancellationToken CancellationToken)
    {
        StorageResult<StorageChangeListResult> ChangesResult = await fStorageProvider.ListChangesAsync(CurrentCheckpoint.StartPageToken, CancellationToken);

        if (ChangesResult.Failed)
            return Result<MetadataSyncRunResult>.Failure(ChangesResult.ErrorText);

        string NextStartPageToken = string.IsNullOrWhiteSpace(ChangesResult.Value.NewStartPageToken)
            ? CurrentCheckpoint.StartPageToken
            : ChangesResult.Value.NewStartPageToken;
        RemoteCheckpointRecord Checkpoint = CreateCheckpoint(Root, NextStartPageToken, RemoteObservedTime);
        MetadataSyncRunResult RunResult = await fSession.AdvanceWithRemoteChangesAndExecuteAsync(
            Root.Id,
            LocalItems,
            ChangesResult.Value.Changes,
            Checkpoint,
            LocalObservedTime,
            RemoteObservedTime,
            CommittedTime,
            ScanId,
            fExecutor,
            CancellationToken);
        RunResult.Kind = MetadataSyncRunKind.Incremental;
        RunResult.LocalObservedItemCount = LocalItems.Count;
        RunResult.RemoteObservedChangeCount = ChangesResult.Value.Changes.Count;

        return Result<MetadataSyncRunResult>.Success(RunResult);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSyncRunner"/> class.
    /// </summary>
    public MetadataSyncRunner(
        SqlMetadataStore Store,
        MetadataSyncSession Session,
        LocalScanner LocalScanner,
        IStorageProvider StorageProvider,
        ISyncExecutor Executor)
    {
        fStore = Guard.NotNull(Store, nameof(Store));
        fSession = Guard.NotNull(Session, nameof(Session));
        fLocalScanner = Guard.NotNull(LocalScanner, nameof(LocalScanner));
        fStorageProvider = Guard.NotNull(StorageProvider, nameof(StorageProvider));
        fExecutor = Guard.NotNull(Executor, nameof(Executor));
    }

    // ● public

    /// <summary>
    /// Runs one metadata synchronization pass for a sync root.
    /// </summary>
    public async Task<Result<MetadataSyncRunResult>> RunOnceAsync(string SyncRootId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));

        SyncRootRecord Root = fStore.GetSyncRoot(SyncRootId);

        if (Root == null)
            return Result<MetadataSyncRunResult>.Failure($"Sync root was not found: {SyncRootId}");

        if (!Root.IsEnabled)
            return Result<MetadataSyncRunResult>.Failure($"Sync root is disabled: {SyncRootId}");

        Result<IReadOnlyList<LocalScanItem>> LocalScanResult = await fLocalScanner.ScanAsync(Root.LocalRootPath, CancellationToken);

        if (LocalScanResult.Failed)
            return Result<MetadataSyncRunResult>.Failure(LocalScanResult.ErrorText);

        DateTime Time = DateTime.UtcNow;
        string ScanId = Guid.NewGuid().ToString("N");
        RemoteCheckpointRecord Checkpoint = fStore.GetRemoteCheckpoint(Root.Id);

        if (Checkpoint == null || string.IsNullOrWhiteSpace(Checkpoint.StartPageToken))
        {
            return await RunBootstrapAsync(
                Root,
                LocalScanResult.Value,
                Time,
                Time,
                Time,
                ScanId,
                CancellationToken);
        }

        return await RunIncrementalAsync(
            Root,
            Checkpoint,
            LocalScanResult.Value,
            Time,
            Time,
            Time,
            ScanId,
            CancellationToken);
    }
}

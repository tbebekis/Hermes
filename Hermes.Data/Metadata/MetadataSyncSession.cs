// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Coordinates metadata classification, planning, and metadata-only commits.
/// </summary>
public class MetadataSyncSession
{
    // ● fields

    readonly SqlMetadataStore fStore;
    readonly SyncPlanner fPlanner;

    // ● private

    IReadOnlyList<TrackedItemDiffRecord> ClassifySyncRootItems(string SyncRootId)
    {
        SyncDiffClassifier Classifier = new();
        List<TrackedItemDiffRecord> Result = new();
        HashSet<string> CollidingItemIds = fStore.FindRemoteNamespaceCollisions(SyncRootId)
            .SelectMany(Item => Item.TrackedItemIds)
            .ToHashSet();

        foreach (TrackedItemRecord Item in fStore.GetTrackedItems(SyncRootId))
        {
            Result.Add(new TrackedItemDiffRecord()
            {
                TrackedItemId = Item.Id,
                DiffKind = Classifier.Classify(fStore.GetDiffInput(Item.Id, CollidingItemIds.Contains(Item.Id))),
            });
        }

        return Result;
    }
    IReadOnlyList<BaseSnapshotRecord> CommitBaseSnapshotsForDecisions(IEnumerable<SyncPlanDecision> Decisions, DateTime CommittedTime)
    {
        List<string> TrackedItemIds = new();

        foreach (SyncPlanDecision Decision in Decisions)
        {
            if (Decision.DecisionKind == SyncPlanDecisionKind.CommitBase)
                TrackedItemIds.Add(Decision.TrackedItemId);
        }

        return fStore.CommitBaseSnapshotsFromObservations(TrackedItemIds, CommittedTime);
    }
    static bool IsPendingExecutorDecision(SyncPlanDecision Decision)
    {
        return Decision.DecisionKind != SyncPlanDecisionKind.None
            && Decision.DecisionKind != SyncPlanDecisionKind.CommitBase;
    }
    static void AddPlanningResult(MetadataSyncSessionResult Target, MetadataSyncSessionResult Source)
    {
        Target.Decisions.AddRange(Source.Decisions);
        Target.CommittedBaseSnapshots.AddRange(Source.CommittedBaseSnapshots);
        Target.PendingExecutorDecisions.AddRange(Source.PendingExecutorDecisions);
        Target.PendingExecutionRequests.AddRange(Source.PendingExecutionRequests);
    }
    static string LocalKey(LocalScanItem Item) => Item.RelativePath;
    static TrackedItemRecord CreateLocalTrackedItem(string SyncRootId, LocalScanItem Item) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        SyncRootId = SyncRootId,
        LocalKey = LocalKey(Item),
        ItemType = Item.ItemType,
    };
    static string ItemType(StorageItem Item) => Item.Kind == StorageItemKind.Folder ? "Folder" : "File";
    static TrackedItemRecord CreateRemoteTrackedItem(string SyncRootId, StorageItem Item) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        SyncRootId = SyncRootId,
        RemoteItemId = Item.Id,
        ItemType = ItemType(Item),
    };
    Dictionary<string, TrackedItemRecord> GetTrackedItemsByLocalKey(string SyncRootId)
    {
        return fStore.GetTrackedItems(SyncRootId)
            .Where(Item => !string.IsNullOrWhiteSpace(Item.LocalKey))
            .ToDictionary(Item => Item.LocalKey);
    }
    Dictionary<string, TrackedItemRecord> GetTrackedItemsByObservedLocalKey(string SyncRootId)
    {
        Dictionary<string, TrackedItemRecord> Result = new();
        Dictionary<string, int> Counts = new();

        foreach (TrackedItemRecord Item in fStore.GetTrackedItems(SyncRootId).Where(Item => string.IsNullOrWhiteSpace(Item.LocalKey)))
        {
            string LocalKey = ExistingLocalPath(fStore.GetLocalObservation(Item.Id), fStore.GetBaseSnapshot(Item.Id));
            if (string.IsNullOrWhiteSpace(LocalKey))
                continue;

            Counts.TryGetValue(LocalKey, out int Count);
            Counts[LocalKey] = Count + 1;
            Result[LocalKey] = Item;
        }

        foreach (string LocalKey in Counts.Where(Item => Item.Value > 1).Select(Item => Item.Key).ToList())
            Result.Remove(LocalKey);

        return Result;
    }
    Dictionary<string, TrackedItemRecord> GetTrackedItemsByRemoteId(string SyncRootId)
    {
        return fStore.GetTrackedItems(SyncRootId)
            .Where(Item => !string.IsNullOrWhiteSpace(Item.RemoteItemId))
            .ToDictionary(Item => Item.RemoteItemId);
    }
    Dictionary<string, string> GetKnownRemoteLocalKeys(string SyncRootId)
    {
        return fStore.GetTrackedItems(SyncRootId)
            .Where(Item => !string.IsNullOrWhiteSpace(Item.RemoteItemId) && !string.IsNullOrWhiteSpace(Item.LocalKey))
            .ToDictionary(Item => Item.RemoteItemId, Item => Item.LocalKey);
    }
    static string ProjectedRemoteLocalKey(SyncRootRecord SyncRoot, StorageItem Item, Dictionary<string, string> KnownRemoteLocalKeys)
    {
        if (SyncRoot == null || Item == null || string.IsNullOrWhiteSpace(Item.Name))
            return null;

        if (string.Equals(Item.ParentId, SyncRoot.RemoteRootItemId, StringComparison.Ordinal))
            return Item.Name;

        if (!KnownRemoteLocalKeys.TryGetValue(Item.ParentId, out string ParentLocalKey))
            return null;

        return string.IsNullOrWhiteSpace(ParentLocalKey)
            ? Item.Name
            : ParentLocalKey + "/" + Item.Name;
    }
    static Dictionary<string, int> CountProjectedRemoteLocalKeys(SyncRootRecord SyncRoot, IEnumerable<StorageItem> Items, Dictionary<string, string> KnownRemoteLocalKeys)
    {
        Dictionary<string, int> Result = new();

        foreach (StorageItem Item in Items)
        {
            string Key = ProjectedRemoteLocalKey(SyncRoot, Item, KnownRemoteLocalKeys);
            if (string.IsNullOrWhiteSpace(Key))
                continue;

            Result.TryGetValue(Key, out int Count);
            Result[Key] = Count + 1;
        }

        return Result;
    }
    static bool SameOptionalText(string A, string B)
    {
        if (string.IsNullOrWhiteSpace(A) || string.IsNullOrWhiteSpace(B))
            return true;

        return string.Equals(A, B, StringComparison.Ordinal);
    }
    static bool SameOptionalSize(long? LocalSize, long RemoteSize)
    {
        if (!LocalSize.HasValue)
            return true;

        return LocalSize.Value == RemoteSize;
    }
    static bool CanAdoptBootstrapItem(StorageItem RemoteItem, LocalObservedSnapshotRecord LocalObservation)
    {
        if (RemoteItem == null || LocalObservation == null || !LocalObservation.ExistsFlag || RemoteItem.Trashed)
            return false;

        string RemoteType = ItemType(RemoteItem);
        if (!string.Equals(LocalObservation.ItemType, RemoteType, StringComparison.Ordinal))
            return false;

        if (RemoteItem.IsFolder)
            return true;

        return SameOptionalSize(LocalObservation.Size, RemoteItem.Size)
            && SameOptionalText(LocalObservation.ContentHash, RemoteItem.Md5Hash);
    }
    bool TryAdoptBootstrapItem(
        RemoteBootstrapResult Result,
        Dictionary<string, TrackedItemRecord> TrackedItemsByRemoteId,
        Dictionary<string, TrackedItemRecord> TrackedItemsByLocalKey,
        Dictionary<string, string> KnownRemoteLocalKeys,
        Dictionary<string, int> ProjectedLocalKeyCounts,
        StorageItem Item,
        string ProjectedLocalKey,
        DateTime ObservedTime,
        out TrackedItemRecord TrackedItem)
    {
        TrackedItem = null;

        if (string.IsNullOrWhiteSpace(ProjectedLocalKey))
            return false;

        if (!ProjectedLocalKeyCounts.TryGetValue(ProjectedLocalKey, out int Count) || Count != 1)
            return false;

        if (!TrackedItemsByLocalKey.TryGetValue(ProjectedLocalKey, out TrackedItem))
            return false;

        if (!string.IsNullOrWhiteSpace(TrackedItem.RemoteItemId))
            return false;

        LocalObservedSnapshotRecord LocalObservation = fStore.GetLocalObservation(TrackedItem.Id);
        if (!CanAdoptBootstrapItem(Item, LocalObservation))
            return false;

        TrackedItem.RemoteItemId = Item.Id;
        TrackedItemsByRemoteId[Item.Id] = TrackedItem;
        KnownRemoteLocalKeys[Item.Id] = ProjectedLocalKey;
        Result.AdoptedTrackedItems.Add(TrackedItem);

        RemoteObservedSnapshotRecord RemoteObservation = RemoteObservationMapper.FromStorageItem(Item, TrackedItem.Id, ObservedTime);
        Result.Observations.Add(RemoteObservation);
        Result.CommittedBaseSnapshots.Add(BaseSnapshotMapper.FromVerifiedObservations(LocalObservation, RemoteObservation, ObservedTime));

        return true;
    }
    static void CheckCheckpointSyncRoot(string SyncRootId, RemoteCheckpointRecord Checkpoint)
    {
        if (!string.Equals(SyncRootId, Checkpoint.SyncRootId, StringComparison.Ordinal))
            throw new ArgumentException("Checkpoint sync root id must match the imported sync root id.", nameof(Checkpoint));
    }
    static string ExistingLocalPath(LocalObservedSnapshotRecord LocalObservation, BaseSnapshotRecord BaseSnapshot)
    {
        if (LocalObservation != null && LocalObservation.ExistsFlag && !string.IsNullOrWhiteSpace(LocalObservation.RelativePath))
            return LocalObservation.RelativePath;

        if (BaseSnapshot != null && BaseSnapshot.ExistsFlag && !string.IsNullOrWhiteSpace(BaseSnapshot.LocalRelativePath))
            return BaseSnapshot.LocalRelativePath;

        return null;
    }
    string ResolveRemoteParentLocalPath(SyncRootRecord SyncRoot, RemoteObservedSnapshotRecord RemoteObservation)
    {
        if (SyncRoot == null || RemoteObservation == null || string.IsNullOrWhiteSpace(RemoteObservation.RemoteParentId))
            return null;

        if (string.Equals(RemoteObservation.RemoteParentId, SyncRoot.RemoteRootItemId, StringComparison.Ordinal))
            return string.Empty;

        TrackedItemRecord ParentItem = fStore.GetTrackedItemByRemoteId(SyncRoot.Id, RemoteObservation.RemoteParentId);
        if (ParentItem == null)
            return null;

        return ExistingLocalPath(fStore.GetLocalObservation(ParentItem.Id), fStore.GetBaseSnapshot(ParentItem.Id));
    }
    string ResolveLocalParentRemoteId(SyncRootRecord SyncRoot, LocalObservedSnapshotRecord LocalObservation)
    {
        if (SyncRoot == null || LocalObservation == null || string.IsNullOrWhiteSpace(LocalObservation.ParentRelativePath))
            return null;

        TrackedItemRecord ParentItem = fStore.GetTrackedItemByLocalKey(SyncRoot.Id, LocalObservation.ParentRelativePath);
        return ParentItem?.RemoteItemId;
    }
    SyncExecutionRequest CreateExecutionRequest(SyncPlanDecision Decision)
    {
        Guard.NotNull(Decision, nameof(Decision));

        TrackedItemRecord TrackedItem = fStore.GetTrackedItem(Decision.TrackedItemId);
        SyncRootRecord SyncRoot = TrackedItem == null ? null : fStore.GetSyncRoot(TrackedItem.SyncRootId);
        LocalObservedSnapshotRecord LocalObservation = fStore.GetLocalObservation(Decision.TrackedItemId);
        RemoteObservedSnapshotRecord RemoteObservation = fStore.GetRemoteObservation(Decision.TrackedItemId);

        return new SyncExecutionRequest()
        {
            Decision = Decision,
            SyncRoot = SyncRoot,
            TrackedItem = TrackedItem,
            BaseSnapshot = fStore.GetBaseSnapshot(Decision.TrackedItemId),
            LocalObservation = LocalObservation,
            RemoteObservation = RemoteObservation,
            RemoteParentLocalRelativePath = ResolveRemoteParentLocalPath(SyncRoot, RemoteObservation),
            LocalParentRemoteItemId = ResolveLocalParentRemoteId(SyncRoot, LocalObservation),
        };
    }
    static bool CanCommitExecutionResult(SyncExecutionResult Result)
    {
        return Result.ResultKind == SyncExecutionResultKind.CompletedAndVerified;
    }
    static bool HasVerifiedCommitObservations(SyncExecutionResult Result)
    {
        return Result.Request?.LocalObservation != null
            && Result.Request.RemoteObservation != null;
    }
    static void MarkMissingCommitObservations(SyncExecutionResult Result)
    {
        if (string.IsNullOrWhiteSpace(Result.Message))
            Result.Message = "Base snapshot cannot be committed until local and remote observations are both available.";
    }
    static string LocalName(string LocalRelativePath)
    {
        if (string.IsNullOrWhiteSpace(LocalRelativePath))
            return string.Empty;

        int Index = LocalRelativePath.LastIndexOf('/');
        return Index < 0 ? LocalRelativePath : LocalRelativePath[(Index + 1)..];
    }
    static string LocalParentRelativePath(string LocalRelativePath)
    {
        if (string.IsNullOrWhiteSpace(LocalRelativePath))
            return null;

        int Index = LocalRelativePath.LastIndexOf('/');
        return Index < 0 ? null : LocalRelativePath[..Index];
    }
    static string RemoteItemType(StorageItem Item)
    {
        if (Item == null)
            return null;

        return Item.Kind == StorageItemKind.Folder ? "Folder" : "File";
    }
    static string RemoteItemHash(StorageItem Item, RemoteObservedSnapshotRecord Observation)
    {
        if (Item != null && !Item.IsFolder)
            return Item.Md5Hash;

        return Observation?.ContentHash;
    }
    static long? RemoteItemSize(StorageItem Item, RemoteObservedSnapshotRecord Observation)
    {
        if (Item != null)
            return Item.IsFolder ? null : Item.Size;

        return Observation?.Size;
    }
    static DateTime? RemoteItemModifiedTime(StorageItem Item, RemoteObservedSnapshotRecord Observation)
    {
        if (Item != null && Item.ModifiedTime != default)
            return Item.ModifiedTime.UtcDateTime;

        return Observation?.ModifiedTime;
    }
    static LocalObservedSnapshotRecord CreateLocalObservationFromExecution(SyncExecutionResult Result, DateTime ObservedTime)
    {
        RemoteObservedSnapshotRecord RemoteObservation = Result.Request.RemoteObservation;

        return new LocalObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId(Result),
            ExistsFlag = true,
            RelativePath = Result.LocalRelativePath,
            Name = LocalName(Result.LocalRelativePath),
            ParentRelativePath = LocalParentRelativePath(Result.LocalRelativePath),
            ItemType = RemoteItemType(Result.RemoteItem) ?? RemoteObservation?.ItemType,
            Size = RemoteItemSize(Result.RemoteItem, RemoteObservation),
            ModifiedTime = RemoteItemModifiedTime(Result.RemoteItem, RemoteObservation),
            ContentHash = RemoteItemHash(Result.RemoteItem, RemoteObservation),
            ObservedTime = ObservedTime,
            ScanId = "execution",
        };
    }
    void ApplyRemoteItemObservation(SyncExecutionResult Result, DateTime ObservedTime)
    {
        if (Result.RemoteItem == null)
            return;

        string ItemId = TrackedItemId(Result);
        TrackedItemRecord TrackedItem = Result.Request.TrackedItem ?? fStore.GetTrackedItem(ItemId);

        if (TrackedItem != null && string.IsNullOrWhiteSpace(TrackedItem.RemoteItemId))
        {
            TrackedItem.RemoteItemId = Result.RemoteItem.Id;
            fStore.UpdateTrackedItem(TrackedItem);
            Result.Request.TrackedItem = TrackedItem;
        }

        RemoteObservedSnapshotRecord Observation = RemoteObservationMapper.FromStorageItem(Result.RemoteItem, ItemId, ObservedTime);
        fStore.UpsertRemoteObservation(Observation);
        Result.Request.RemoteObservation = Observation;
    }
    void ApplyLocalPathObservation(SyncExecutionResult Result, DateTime ObservedTime)
    {
        if (Result.Request.LocalObservation != null || string.IsNullOrWhiteSpace(Result.LocalRelativePath))
            return;

        string ItemId = TrackedItemId(Result);
        TrackedItemRecord TrackedItem = Result.Request.TrackedItem ?? fStore.GetTrackedItem(ItemId);

        if (TrackedItem != null && string.IsNullOrWhiteSpace(TrackedItem.LocalKey))
        {
            TrackedItem.LocalKey = Result.LocalRelativePath;
            fStore.UpdateTrackedItem(TrackedItem);
            Result.Request.TrackedItem = TrackedItem;
        }

        LocalObservedSnapshotRecord Observation = CreateLocalObservationFromExecution(Result, ObservedTime);
        fStore.UpsertLocalObservation(Observation);
        Result.Request.LocalObservation = Observation;
    }
    static string TrackedItemId(SyncExecutionResult Result)
    {
        Guard.NotNull(Result, nameof(Result));
        Guard.NotNull(Result.Request, nameof(Result.Request));
        Guard.NotNull(Result.Request.Decision, nameof(Result.Request.Decision));

        return Guard.NotNullOrWhiteSpace(Result.Request.Decision.TrackedItemId, nameof(Result.Request.Decision.TrackedItemId));
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSyncSession"/> class.
    /// </summary>
    public MetadataSyncSession(SqlMetadataStore Store, SyncPlanner Planner)
    {
        fStore = Store ?? throw new ArgumentNullException(nameof(Store));
        fPlanner = Planner ?? throw new ArgumentNullException(nameof(Planner));
    }

    // ● public

    /// <summary>
    /// Imports a full local scan into metadata.
    /// </summary>
    public LocalScanImportResult ImportLocalScan(string SyncRootId, IEnumerable<LocalScanItem> Items, DateTime ObservedTime, string ScanId)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));
        Guard.NotNull(Items, nameof(Items));

        LocalScanImportResult Result = new();
        Dictionary<string, TrackedItemRecord> TrackedItemsByLocalKey = GetTrackedItemsByLocalKey(SyncRootId);
        Dictionary<string, TrackedItemRecord> TrackedItemsByObservedLocalKey = GetTrackedItemsByObservedLocalKey(SyncRootId);
        HashSet<string> ObservedLocalKeys = new();

        foreach (LocalScanItem Item in Items)
        {
            string Key = LocalKey(Item);
            ObservedLocalKeys.Add(Key);

            if (!TrackedItemsByLocalKey.TryGetValue(Key, out TrackedItemRecord TrackedItem))
            {
                if (TrackedItemsByObservedLocalKey.TryGetValue(Key, out TrackedItem))
                {
                    TrackedItem.LocalKey = Key;
                    TrackedItemsByLocalKey[Key] = TrackedItem;
                    fStore.UpdateTrackedItem(TrackedItem);
                }
                else
                {
                    TrackedItem = CreateLocalTrackedItem(SyncRootId, Item);
                    TrackedItemsByLocalKey[Key] = TrackedItem;
                    Result.CreatedTrackedItems.Add(TrackedItem);
                }
            }

            Result.Observations.Add(LocalObservationMapper.FromScanItem(Item, TrackedItem.Id, ObservedTime, ScanId));
        }

        foreach (TrackedItemRecord TrackedItem in TrackedItemsByLocalKey.Values)
        {
            if (!ObservedLocalKeys.Contains(TrackedItem.LocalKey))
                Result.Observations.Add(LocalObservationMapper.Missing(TrackedItem.Id, ObservedTime, ScanId));
        }

        fStore.SaveLocalScanImportResult(Result);

        return Result;
    }
    /// <summary>
    /// Imports a full remote snapshot into metadata.
    /// </summary>
    public RemoteBootstrapResult ImportRemoteSnapshot(string SyncRootId, IEnumerable<StorageItem> Items, RemoteCheckpointRecord Checkpoint, DateTime ObservedTime)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));
        Guard.NotNull(Items, nameof(Items));
        Guard.NotNull(Checkpoint, nameof(Checkpoint));
        CheckCheckpointSyncRoot(SyncRootId, Checkpoint);

        SyncRootRecord SyncRoot = fStore.GetSyncRoot(SyncRootId);
        List<StorageItem> ItemList = Items.ToList();
        RemoteBootstrapResult Result = new();
        Dictionary<string, TrackedItemRecord> TrackedItemsByRemoteId = GetTrackedItemsByRemoteId(SyncRootId);
        Dictionary<string, TrackedItemRecord> TrackedItemsByLocalKey = GetTrackedItemsByLocalKey(SyncRootId);
        Dictionary<string, string> KnownRemoteLocalKeys = GetKnownRemoteLocalKeys(SyncRootId);
        Dictionary<string, int> ProjectedLocalKeyCounts = CountProjectedRemoteLocalKeys(SyncRoot, ItemList, KnownRemoteLocalKeys);

        foreach (StorageItem Item in ItemList)
        {
            if (!TrackedItemsByRemoteId.TryGetValue(Item.Id, out TrackedItemRecord TrackedItem))
            {
                string ProjectedLocalKey = ProjectedRemoteLocalKey(SyncRoot, Item, KnownRemoteLocalKeys);
                if (!TryAdoptBootstrapItem(
                    Result,
                    TrackedItemsByRemoteId,
                    TrackedItemsByLocalKey,
                    KnownRemoteLocalKeys,
                    ProjectedLocalKeyCounts,
                    Item,
                    ProjectedLocalKey,
                    ObservedTime,
                    out TrackedItem))
                {
                    TrackedItem = CreateRemoteTrackedItem(SyncRootId, Item);
                    TrackedItemsByRemoteId[Item.Id] = TrackedItem;
                    Result.CreatedTrackedItems.Add(TrackedItem);
                }
            }

            if (!Result.AdoptedTrackedItems.Contains(TrackedItem))
                Result.Observations.Add(RemoteObservationMapper.FromStorageItem(Item, TrackedItem.Id, ObservedTime));
        }

        fStore.SaveRemoteBootstrapResultWithCheckpoint(Result, Checkpoint);

        return Result;
    }
    /// <summary>
    /// Imports remote provider changes into metadata.
    /// </summary>
    public RemoteChangeImportResult ImportRemoteChanges(string SyncRootId, IEnumerable<StorageChange> Changes, RemoteCheckpointRecord Checkpoint, DateTime ObservedTime)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));
        Guard.NotNull(Changes, nameof(Changes));
        Guard.NotNull(Checkpoint, nameof(Checkpoint));
        CheckCheckpointSyncRoot(SyncRootId, Checkpoint);

        RemoteChangeImportResult Result = new();
        Dictionary<string, TrackedItemRecord> TrackedItemsByRemoteId = GetTrackedItemsByRemoteId(SyncRootId);

        foreach (StorageChange Change in Changes)
        {
            if (!TrackedItemsByRemoteId.TryGetValue(Change.ItemId, out TrackedItemRecord TrackedItem))
            {
                if (Change.Item != null && !Change.Removed)
                {
                    TrackedItem = CreateRemoteTrackedItem(SyncRootId, Change.Item);
                    TrackedItemsByRemoteId[Change.ItemId] = TrackedItem;
                    Result.CreatedTrackedItems.Add(TrackedItem);
                }
                else
                {
                    Result.UntrackedChanges.Add(Change);
                    continue;
                }
            }

            Result.Observations.Add(RemoteObservationMapper.FromChange(Change, TrackedItem.Id, ObservedTime));
        }

        if (Result.UntrackedChanges.Count == 0)
            fStore.SaveRemoteChangeImportResultWithCheckpoint(Result, Checkpoint);

        return Result;
    }
    /// <summary>
    /// Classifies all tracked items in a sync root.
    /// </summary>
    public IReadOnlyList<TrackedItemDiffRecord> ClassifySyncRoot(string SyncRootId)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));

        return ClassifySyncRootItems(SyncRootId);
    }
    /// <summary>
    /// Creates planner inputs for all tracked items in a sync root.
    /// </summary>
    public IReadOnlyList<SyncPlanInput> CreatePlanInputs(string SyncRootId)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));

        List<SyncPlanInput> Result = new();

        foreach (TrackedItemDiffRecord Diff in ClassifySyncRootItems(SyncRootId))
        {
            Result.Add(new SyncPlanInput()
            {
                TrackedItemId = Diff.TrackedItemId,
                DiffKind = Diff.DiffKind,
            });
        }

        return Result;
    }
    /// <summary>
    /// Creates planner decisions for all tracked items in a sync root.
    /// </summary>
    public IReadOnlyList<SyncPlanDecision> CreatePlanDecisions(string SyncRootId)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));

        return fPlanner.CreateDecisions(CreatePlanInputs(SyncRootId));
    }
    /// <summary>
    /// Commits base snapshots for planner decisions that require metadata-only base advancement.
    /// </summary>
    public IReadOnlyList<BaseSnapshotRecord> CommitBaseDecisions(string SyncRootId, DateTime CommittedTime)
    {
        return AdvanceMetadataOnly(SyncRootId, CommittedTime).CommittedBaseSnapshots;
    }
    /// <summary>
    /// Creates planner decisions and commits metadata-only base advancements.
    /// </summary>
    public MetadataSyncSessionResult AdvanceMetadataOnly(string SyncRootId, DateTime CommittedTime)
    {
        MetadataSyncSessionResult Result = new();
        IReadOnlyList<SyncPlanDecision> Decisions = CreatePlanDecisions(SyncRootId);
        IReadOnlyList<BaseSnapshotRecord> CommittedBaseSnapshots = CommitBaseSnapshotsForDecisions(Decisions, CommittedTime);

        Result.Decisions.AddRange(Decisions);
        Result.CommittedBaseSnapshots.AddRange(CommittedBaseSnapshots);
        Result.PendingExecutorDecisions.AddRange(Decisions.Where(IsPendingExecutorDecision));

        foreach (SyncPlanDecision Decision in Result.PendingExecutorDecisions)
            Result.PendingExecutionRequests.Add(CreateExecutionRequest(Decision));

        return Result;
    }
    /// <summary>
    /// Applies execution results and commits base snapshots for results that completed and were verified.
    /// </summary>
    public SyncExecutionApplyResult ApplyExecutionResults(IEnumerable<SyncExecutionResult> Results, DateTime CommittedTime)
    {
        Guard.NotNull(Results, nameof(Results));

        SyncExecutionApplyResult Result = new();
        List<string> TrackedItemIds = new();

        foreach (SyncExecutionResult ExecutionResult in Results)
        {
            Guard.NotNull(ExecutionResult, nameof(Results));

            if (CanCommitExecutionResult(ExecutionResult))
            {
                ApplyRemoteItemObservation(ExecutionResult, CommittedTime);
                ApplyLocalPathObservation(ExecutionResult, CommittedTime);
            }

            if (CanCommitExecutionResult(ExecutionResult) && HasVerifiedCommitObservations(ExecutionResult))
            {
                TrackedItemIds.Add(TrackedItemId(ExecutionResult));
                Result.CommittedResults.Add(ExecutionResult);
            }
            else
            {
                if (CanCommitExecutionResult(ExecutionResult))
                    MarkMissingCommitObservations(ExecutionResult);

                Result.UncommittedResults.Add(ExecutionResult);
            }
        }

        Result.CommittedBaseSnapshots.AddRange(fStore.CommitBaseSnapshotsFromObservations(TrackedItemIds, CommittedTime));

        return Result;
    }
    /// <summary>
    /// Commits base snapshots for execution results that completed and were verified.
    /// </summary>
    public IReadOnlyList<BaseSnapshotRecord> CommitVerifiedExecutionResults(IEnumerable<SyncExecutionResult> Results, DateTime CommittedTime)
    {
        return ApplyExecutionResults(Results, CommittedTime).CommittedBaseSnapshots;
    }
    /// <summary>
    /// Executes pending synchronization requests and applies their execution results.
    /// </summary>
    public async Task<SyncExecutionApplyResult> ExecutePendingRequestsAsync(
        IEnumerable<SyncExecutionRequest> Requests,
        ISyncExecutor Executor,
        DateTime CommittedTime,
        CancellationToken CancellationToken)
    {
        Guard.NotNull(Requests, nameof(Requests));
        Guard.NotNull(Executor, nameof(Executor));

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(Requests, CancellationToken);

        return ApplyExecutionResults(Results, CommittedTime);
    }
    /// <summary>
    /// Executes pending synchronization requests from a session result and applies their execution results.
    /// </summary>
    public Task<SyncExecutionApplyResult> ExecutePendingRequestsAsync(
        MetadataSyncSessionResult Result,
        ISyncExecutor Executor,
        DateTime CommittedTime,
        CancellationToken CancellationToken)
    {
        Guard.NotNull(Result, nameof(Result));

        return ExecutePendingRequestsAsync(Result.PendingExecutionRequests, Executor, CommittedTime, CancellationToken);
    }
    /// <summary>
    /// Imports local and full remote observations, creates decisions, executes pending requests, and applies execution results.
    /// </summary>
    public async Task<MetadataSyncRunResult> AdvanceWithRemoteSnapshotAndExecuteAsync(
        string SyncRootId,
        IEnumerable<LocalScanItem> LocalItems,
        IEnumerable<StorageItem> RemoteItems,
        RemoteCheckpointRecord Checkpoint,
        DateTime LocalObservedTime,
        DateTime RemoteObservedTime,
        DateTime CommittedTime,
        string ScanId,
        ISyncExecutor Executor,
        CancellationToken CancellationToken)
    {
        Guard.NotNull(Executor, nameof(Executor));

        MetadataSyncSessionResult SessionResult = AdvanceWithRemoteSnapshot(SyncRootId, LocalItems, RemoteItems, Checkpoint, LocalObservedTime, RemoteObservedTime, CommittedTime, ScanId);
        MetadataSyncRunResult Result = new()
        {
            SessionResult = SessionResult,
        };

        if (SessionResult.PendingExecutionRequests.Count != 0)
            Result.ExecutionApplyResult = await ExecutePendingRequestsAsync(SessionResult, Executor, CommittedTime, CancellationToken);

        return Result;
    }
    /// <summary>
    /// Imports local observations and remote changes, creates decisions, executes pending requests, and applies execution results.
    /// </summary>
    public async Task<MetadataSyncRunResult> AdvanceWithRemoteChangesAndExecuteAsync(
        string SyncRootId,
        IEnumerable<LocalScanItem> LocalItems,
        IEnumerable<StorageChange> RemoteChanges,
        RemoteCheckpointRecord Checkpoint,
        DateTime LocalObservedTime,
        DateTime RemoteObservedTime,
        DateTime CommittedTime,
        string ScanId,
        ISyncExecutor Executor,
        CancellationToken CancellationToken)
    {
        Guard.NotNull(Executor, nameof(Executor));

        MetadataSyncSessionResult SessionResult = AdvanceWithRemoteChanges(SyncRootId, LocalItems, RemoteChanges, Checkpoint, LocalObservedTime, RemoteObservedTime, CommittedTime, ScanId);
        MetadataSyncRunResult Result = new()
        {
            SessionResult = SessionResult,
        };

        if (SessionResult.PendingExecutionRequests.Count != 0)
            Result.ExecutionApplyResult = await ExecutePendingRequestsAsync(SessionResult, Executor, CommittedTime, CancellationToken);

        return Result;
    }
    /// <summary>
    /// Imports local and full remote observations, creates decisions, and commits metadata-only base advancements.
    /// </summary>
    public MetadataSyncSessionResult AdvanceWithRemoteSnapshot(
        string SyncRootId,
        IEnumerable<LocalScanItem> LocalItems,
        IEnumerable<StorageItem> RemoteItems,
        RemoteCheckpointRecord Checkpoint,
        DateTime LocalObservedTime,
        DateTime RemoteObservedTime,
        DateTime CommittedTime,
        string ScanId)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));
        Guard.NotNull(LocalItems, nameof(LocalItems));
        Guard.NotNull(RemoteItems, nameof(RemoteItems));
        Guard.NotNull(Checkpoint, nameof(Checkpoint));

        MetadataSyncSessionResult Result = new();
        LocalScanImportResult LocalImport = ImportLocalScan(SyncRootId, LocalItems, LocalObservedTime, ScanId);
        RemoteBootstrapResult RemoteImport = ImportRemoteSnapshot(SyncRootId, RemoteItems, Checkpoint, RemoteObservedTime);
        MetadataSyncSessionResult AdvanceResult = AdvanceMetadataOnly(SyncRootId, CommittedTime);

        Result.CreatedTrackedItems.AddRange(LocalImport.CreatedTrackedItems);
        Result.CreatedTrackedItems.AddRange(RemoteImport.CreatedTrackedItems);
        Result.CommittedBaseSnapshots.AddRange(RemoteImport.CommittedBaseSnapshots);
        AddPlanningResult(Result, AdvanceResult);

        return Result;
    }
    /// <summary>
    /// Imports local observations and remote changes, creates decisions, and commits metadata-only base advancements.
    /// </summary>
    public MetadataSyncSessionResult AdvanceWithRemoteChanges(
        string SyncRootId,
        IEnumerable<LocalScanItem> LocalItems,
        IEnumerable<StorageChange> RemoteChanges,
        RemoteCheckpointRecord Checkpoint,
        DateTime LocalObservedTime,
        DateTime RemoteObservedTime,
        DateTime CommittedTime,
        string ScanId)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));
        Guard.NotNull(LocalItems, nameof(LocalItems));
        Guard.NotNull(RemoteChanges, nameof(RemoteChanges));
        Guard.NotNull(Checkpoint, nameof(Checkpoint));

        MetadataSyncSessionResult Result = new();
        RemoteChangeImportResult RemoteImport = ImportRemoteChanges(SyncRootId, RemoteChanges, Checkpoint, RemoteObservedTime);

        Result.CreatedTrackedItems.AddRange(RemoteImport.CreatedTrackedItems);
        Result.UntrackedRemoteChanges.AddRange(RemoteImport.UntrackedChanges);

        if (Result.UntrackedRemoteChanges.Count != 0)
            return Result;

        LocalScanImportResult LocalImport = ImportLocalScan(SyncRootId, LocalItems, LocalObservedTime, ScanId);
        Result.CreatedTrackedItems.AddRange(LocalImport.CreatedTrackedItems);
        AddPlanningResult(Result, AdvanceMetadataOnly(SyncRootId, CommittedTime));

        return Result;
    }
}

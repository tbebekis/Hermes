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
    Dictionary<string, TrackedItemRecord> GetTrackedItemsByRemoteId(string SyncRootId)
    {
        return fStore.GetTrackedItems(SyncRootId)
            .Where(Item => !string.IsNullOrWhiteSpace(Item.RemoteItemId))
            .ToDictionary(Item => Item.RemoteItemId);
    }
    static void CheckCheckpointSyncRoot(string SyncRootId, RemoteCheckpointRecord Checkpoint)
    {
        if (!string.Equals(SyncRootId, Checkpoint.SyncRootId, StringComparison.Ordinal))
            throw new ArgumentException("Checkpoint sync root id must match the imported sync root id.", nameof(Checkpoint));
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
        HashSet<string> ObservedLocalKeys = new();

        foreach (LocalScanItem Item in Items)
        {
            string Key = LocalKey(Item);
            ObservedLocalKeys.Add(Key);

            if (!TrackedItemsByLocalKey.TryGetValue(Key, out TrackedItemRecord TrackedItem))
            {
                TrackedItem = CreateLocalTrackedItem(SyncRootId, Item);
                TrackedItemsByLocalKey[Key] = TrackedItem;
                Result.CreatedTrackedItems.Add(TrackedItem);
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

        RemoteBootstrapResult Result = new();
        Dictionary<string, TrackedItemRecord> TrackedItemsByRemoteId = GetTrackedItemsByRemoteId(SyncRootId);

        foreach (StorageItem Item in Items)
        {
            if (!TrackedItemsByRemoteId.TryGetValue(Item.Id, out TrackedItemRecord TrackedItem))
            {
                TrackedItem = CreateRemoteTrackedItem(SyncRootId, Item);
                TrackedItemsByRemoteId[Item.Id] = TrackedItem;
                Result.CreatedTrackedItems.Add(TrackedItem);
            }

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

        return Result;
    }
}

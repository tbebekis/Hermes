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
        SyncRootRecord SyncRoot = fStore.GetSyncRoot(SyncRootId);
        HashSet<string> CollidingItemIds = fStore.FindRemoteNamespaceCollisions(SyncRootId)
            .SelectMany(Item => Item.TrackedItemIds)
            .ToHashSet();
        CollidingItemIds.UnionWith(FindProjectedLocalNamespaceCollisionItemIds(SyncRoot));

        foreach (TrackedItemRecord Item in fStore.GetTrackedItems(SyncRootId))
        {
            BaseSnapshotRecord BaseSnapshot = fStore.GetBaseSnapshot(Item.Id);
            LocalObservedSnapshotRecord LocalObservation = fStore.GetLocalObservation(Item.Id);
            RemoteObservedSnapshotRecord RemoteObservation = fStore.GetRemoteObservation(Item.Id);
            Result.Add(new TrackedItemDiffRecord()
            {
                TrackedItemId = Item.Id,
                DiffKind = Classifier.Classify(SyncItemStateMapper.CreateDiffInput(
                    BaseSnapshot,
                    LocalObservation,
                    RemoteObservation,
                    CollidingItemIds.Contains(Item.Id),
                    ProjectRemoteLocalPath(SyncRoot, RemoteObservation))),
            });
        }

        return Result;
    }
    HashSet<string> FindProjectedLocalNamespaceCollisionItemIds(SyncRootRecord SyncRoot)
    {
        Dictionary<string, HashSet<string>> Map = new(StringComparer.Ordinal);

        if (SyncRoot == null)
            return new HashSet<string>();

        foreach (TrackedItemRecord Item in fStore.GetTrackedItems(SyncRoot.Id))
        {
            LocalObservedSnapshotRecord LocalObservation = fStore.GetLocalObservation(Item.Id);
            RemoteObservedSnapshotRecord RemoteObservation = fStore.GetRemoteObservation(Item.Id);

            if (LocalObservation != null && LocalObservation.ExistsFlag && !string.IsNullOrWhiteSpace(LocalObservation.RelativePath))
                AddProjectedLocalNamespaceItem(Map, LocalObservation.RelativePath, Item.Id);

            if (!IsInactiveRemoteObservation(RemoteObservation))
                AddProjectedLocalNamespaceItem(Map, ProjectRemoteLocalPath(SyncRoot, RemoteObservation), Item.Id);
        }

        return Map.Values
            .Where(Item => Item.Count > 1)
            .SelectMany(Item => Item)
            .ToHashSet();
    }
    static void AddProjectedLocalNamespaceItem(Dictionary<string, HashSet<string>> Map, string LocalPath, string TrackedItemId)
    {
        if (string.IsNullOrWhiteSpace(LocalPath) || string.IsNullOrWhiteSpace(TrackedItemId))
            return;

        if (!Map.TryGetValue(LocalPath, out HashSet<string> Items))
        {
            Items = new HashSet<string>(StringComparer.Ordinal);
            Map[LocalPath] = Items;
        }

        Items.Add(TrackedItemId);
    }
    static bool IsPendingExecutorDecision(SyncPlanDecision Decision)
    {
        return Decision.DecisionKind != SyncPlanDecisionKind.None
            && Decision.DecisionKind != SyncPlanDecisionKind.CommitBase;
    }
    static bool IsDurableConflictDecision(SyncPlanDecision Decision)
    {
        return Decision.DecisionKind == SyncPlanDecisionKind.Conflict
            || Decision.DecisionKind == SyncPlanDecisionKind.Blocked;
    }
    static string ConflictMessage(SyncPlanDecision Decision)
    {
        return Decision.DecisionKind == SyncPlanDecisionKind.Blocked
            ? "Synchronization is blocked by a namespace collision."
            : "Conflict resolution is required.";
    }
    static SyncConflictRecord CreateOpenConflict(string SyncRootId, SyncPlanDecision Decision, DateTime ObservedTime) => new()
    {
        SyncRootId = SyncRootId,
        TrackedItemId = Decision.TrackedItemId,
        DiffKind = Decision.DiffKind,
        DecisionKind = Decision.DecisionKind,
        State = SyncConflictState.Open,
        Message = ConflictMessage(Decision),
        FirstObservedTime = ObservedTime,
        LastObservedTime = ObservedTime,
    };
    IReadOnlyList<BaseSnapshotRecord> SavePlanningSideEffects(string SyncRootId, IEnumerable<SyncPlanDecision> Decisions, DateTime CommittedTime)
    {
        List<string> CommitBaseTrackedItemIds = new();
        List<SyncConflictRecord> OpenConflicts = new();
        List<string> ResolvedConflictTrackedItemIds = new();

        foreach (SyncPlanDecision Decision in Decisions)
        {
            if (Decision.DecisionKind == SyncPlanDecisionKind.CommitBase)
                CommitBaseTrackedItemIds.Add(Decision.TrackedItemId);

            if (IsDurableConflictDecision(Decision))
                OpenConflicts.Add(CreateOpenConflict(SyncRootId, Decision, CommittedTime));
            else
                ResolvedConflictTrackedItemIds.Add(Decision.TrackedItemId);
        }

        return fStore.SavePlanningSideEffects(CommitBaseTrackedItemIds, OpenConflicts, ResolvedConflictTrackedItemIds, CommittedTime);
    }
    static void AddPlanningResult(MetadataSyncSessionResult Target, MetadataSyncSessionResult Source)
    {
        Target.Decisions.AddRange(Source.Decisions);
        Target.CommittedBaseSnapshots.AddRange(Source.CommittedBaseSnapshots);
        Target.PendingExecutorDecisions.AddRange(Source.PendingExecutorDecisions);
        Target.PendingExecutionRequests.AddRange(Source.PendingExecutionRequests);
    }
    static void AddExecutionApplyResult(SyncExecutionApplyResult Target, SyncExecutionApplyResult Source)
    {
        Target.CommittedResults.AddRange(Source.CommittedResults);
        Target.UncommittedResults.AddRange(Source.UncommittedResults);
        Target.CommittedBaseSnapshots.AddRange(Source.CommittedBaseSnapshots);
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
    static string ParentLocalPath(string LocalRelativePath)
    {
        if (string.IsNullOrWhiteSpace(LocalRelativePath))
            return string.Empty;

        int Index = LocalRelativePath.LastIndexOf('/');
        return Index < 0 ? string.Empty : LocalRelativePath[..Index];
    }
    static int LocalPathDepth(string LocalRelativePath)
    {
        if (string.IsNullOrWhiteSpace(LocalRelativePath))
            return 0;

        return LocalRelativePath.Count(Char => Char == '/');
    }
    static bool CanAdoptLocalNamespaceChange(LocalScanItem ScanItem, TrackedItemRecord TrackedItem, BaseSnapshotRecord BaseSnapshot)
    {
        if (ScanItem == null || TrackedItem == null || BaseSnapshot == null || !BaseSnapshot.ExistsFlag)
            return false;

        if (!string.Equals(TrackedItem.ItemType, ScanItem.ItemType, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(TrackedItem.RemoteItemId))
            return false;

        if (string.Equals(ScanItem.ItemType, "Folder", StringComparison.Ordinal))
            return string.Equals(BaseSnapshot.ItemType, "Folder", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(ScanItem.ContentHash) || !ScanItem.Size.HasValue)
            return false;

        return string.Equals(BaseSnapshot.ItemType, "File", StringComparison.Ordinal)
            && string.Equals(BaseSnapshot.ContentHash, ScanItem.ContentHash, StringComparison.Ordinal)
            && BaseSnapshot.Size == ScanItem.Size;
    }
    TrackedItemRecord FindLocalNamespaceCandidate(string SyncRootId, LocalScanItem ScanItem, HashSet<string> ObservedLocalKeys, HashSet<string> UsedTrackedItemIds)
    {
        List<TrackedItemRecord> Candidates = new();
        List<TrackedItemRecord> NameMatchedCandidates = new();
        List<TrackedItemRecord> ParentMatchedCandidates = new();

        foreach (TrackedItemRecord TrackedItem in fStore.GetTrackedItems(SyncRootId))
        {
            if (string.IsNullOrWhiteSpace(TrackedItem.LocalKey)
                || ObservedLocalKeys.Contains(TrackedItem.LocalKey)
                || UsedTrackedItemIds.Contains(TrackedItem.Id))
                continue;

            if (CanAdoptLocalNamespaceChange(ScanItem, TrackedItem, fStore.GetBaseSnapshot(TrackedItem.Id)))
            {
                Candidates.Add(TrackedItem);

                if (string.Equals(ScanItem.ItemType, "Folder", StringComparison.Ordinal))
                {
                    BaseSnapshotRecord BaseSnapshot = fStore.GetBaseSnapshot(TrackedItem.Id);
                    if (string.Equals(BaseSnapshot?.Name, ScanItem.Name, StringComparison.Ordinal))
                        NameMatchedCandidates.Add(TrackedItem);
                    if (string.Equals(ParentLocalPath(BaseSnapshot?.LocalRelativePath), ScanItem.ParentRelativePath ?? string.Empty, StringComparison.Ordinal))
                        ParentMatchedCandidates.Add(TrackedItem);
                }
            }
        }

        if (NameMatchedCandidates.Count == 1)
            return NameMatchedCandidates[0];
        if (ParentMatchedCandidates.Count == 1)
            return ParentMatchedCandidates[0];

        return Candidates.Count == 1 ? Candidates[0] : null;
    }
    static TrackedItemRecord FindLocalNamespaceDescendantCandidate(
        LocalScanItem ScanItem,
        Dictionary<string, TrackedItemRecord> TrackedItemsByLocalKey,
        IEnumerable<(string OldPrefix, string NewPrefix)> NamespacePrefixMaps,
        HashSet<string> ObservedLocalKeys,
        HashSet<string> UsedTrackedItemIds)
    {
        string Key = LocalKey(ScanItem);

        foreach ((string OldPrefix, string NewPrefix) in NamespacePrefixMaps)
        {
            if (!IsDescendantPath(NewPrefix, Key))
                continue;

            string OldKey = ReplacePathPrefix(Key, NewPrefix, OldPrefix);
            if (!TrackedItemsByLocalKey.TryGetValue(OldKey, out TrackedItemRecord TrackedItem))
                continue;

            if (ObservedLocalKeys.Contains(TrackedItem.LocalKey) || UsedTrackedItemIds.Contains(TrackedItem.Id))
                continue;

            if (string.Equals(TrackedItem.ItemType, ScanItem.ItemType, StringComparison.Ordinal))
                return TrackedItem;
        }

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
    string ProjectRemoteLocalPath(SyncRootRecord SyncRoot, RemoteObservedSnapshotRecord RemoteObservation)
    {
        if (SyncRoot == null || RemoteObservation == null || string.IsNullOrWhiteSpace(RemoteObservation.Name))
            return null;

        if (string.Equals(RemoteObservation.RemoteParentId, SyncRoot.RemoteRootItemId, StringComparison.Ordinal))
            return RemoteObservation.Name;

        string ParentPath = ResolveRemoteParentLocalPath(SyncRoot, RemoteObservation);
        if (ParentPath == null)
            return null;

        return string.IsNullOrWhiteSpace(ParentPath)
            ? RemoteObservation.Name
            : ParentPath + "/" + RemoteObservation.Name;
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
    SyncExecutionRequest RefreshExecutionRequest(SyncExecutionRequest Request)
    {
        if (!string.IsNullOrWhiteSpace(Request?.Decision?.TrackedItemId) && fStore.GetTrackedItem(Request.Decision.TrackedItemId) != null)
            return CreateExecutionRequest(Request.Decision);

        return Request;
    }
    static string RequestRemoteItemId(SyncExecutionRequest Request)
    {
        if (!string.IsNullOrWhiteSpace(Request?.TrackedItem?.RemoteItemId))
            return Request.TrackedItem.RemoteItemId;

        if (!string.IsNullOrWhiteSpace(Request?.RemoteObservation?.RemoteItemId))
            return Request.RemoteObservation.RemoteItemId;

        return string.Empty;
    }
    static string RequestLocalPath(SyncExecutionRequest Request)
    {
        if (!string.IsNullOrWhiteSpace(Request?.LocalObservation?.RelativePath))
            return Request.LocalObservation.RelativePath;

        if (!string.IsNullOrWhiteSpace(Request?.BaseSnapshot?.LocalRelativePath))
            return Request.BaseSnapshot.LocalRelativePath;

        return string.Empty;
    }
    static int RemoteDependencyDepth(SyncExecutionRequest Request, Dictionary<string, SyncExecutionRequest> RequestsByRemoteId, Dictionary<string, int> Cache)
    {
        string RemoteItemId = RequestRemoteItemId(Request);
        if (string.IsNullOrWhiteSpace(RemoteItemId))
            return 0;

        if (Cache.TryGetValue(RemoteItemId, out int Cached))
            return Cached;

        string ParentId = Request?.RemoteObservation?.RemoteParentId;
        int Result = !string.IsNullOrWhiteSpace(ParentId) && RequestsByRemoteId.TryGetValue(ParentId, out SyncExecutionRequest Parent)
            ? RemoteDependencyDepth(Parent, RequestsByRemoteId, Cache) + 1
            : 0;

        Cache[RemoteItemId] = Result;
        return Result;
    }
    static int LocalDependencyDepth(SyncExecutionRequest Request, Dictionary<string, SyncExecutionRequest> RequestsByLocalPath, Dictionary<string, int> Cache)
    {
        string LocalPath = RequestLocalPath(Request);
        if (string.IsNullOrWhiteSpace(LocalPath))
            return 0;

        if (Cache.TryGetValue(LocalPath, out int Cached))
            return Cached;

        string ParentPath = Request?.LocalObservation?.ParentRelativePath;
        int Result = !string.IsNullOrWhiteSpace(ParentPath) && RequestsByLocalPath.TryGetValue(ParentPath, out SyncExecutionRequest Parent)
            ? LocalDependencyDepth(Parent, RequestsByLocalPath, Cache) + 1
            : 0;

        Cache[LocalPath] = Result;
        return Result;
    }
    static IReadOnlyList<SyncExecutionRequest> OrderExecutionRequests(IEnumerable<SyncExecutionRequest> Requests)
    {
        List<SyncExecutionRequest> Result = Requests.ToList();
        Dictionary<string, SyncExecutionRequest> RequestsByRemoteId = Result
            .Select(Item => new { RemoteItemId = RequestRemoteItemId(Item), Request = Item })
            .Where(Item => !string.IsNullOrWhiteSpace(Item.RemoteItemId))
            .ToDictionary(Item => Item.RemoteItemId, Item => Item.Request);
        Dictionary<string, SyncExecutionRequest> RequestsByLocalPath = Result
            .Select(Item => new { LocalPath = RequestLocalPath(Item), Request = Item })
            .Where(Item => !string.IsNullOrWhiteSpace(Item.LocalPath))
            .GroupBy(Item => Item.LocalPath, StringComparer.Ordinal)
            .Where(Group => Group.Count() == 1)
            .ToDictionary(Group => Group.Key, Group => Group.First().Request, StringComparer.Ordinal);
        Dictionary<string, int> RemoteCache = new();
        Dictionary<string, int> LocalCache = new();

        return Result
            .OrderBy(Item => RemoteDependencyDepth(Item, RequestsByRemoteId, RemoteCache))
            .ThenBy(Item => LocalDependencyDepth(Item, RequestsByLocalPath, LocalCache))
            .ToList();
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
    static bool IsRemoteDeletePropagation(SyncExecutionResult Result)
    {
        return Result.Request?.Decision?.DecisionKind == SyncPlanDecisionKind.PropagateRemoteDelete;
    }
    static bool IsRemoteNamespaceApply(SyncExecutionResult Result)
    {
        return Result.Request?.Decision?.DecisionKind == SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal;
    }
    static bool IsLocalNamespaceApply(SyncExecutionResult Result)
    {
        return Result.Request?.Decision?.DecisionKind == SyncPlanDecisionKind.ApplyLocalNamespaceToRemote;
    }
    static bool IsFolder(SyncExecutionResult Result)
    {
        return string.Equals(Result.Request?.TrackedItem?.ItemType, "Folder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Result.Request?.LocalObservation?.ItemType, "Folder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Result.Request?.RemoteObservation?.ItemType, "Folder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Result.Request?.BaseSnapshot?.ItemType, "Folder", StringComparison.OrdinalIgnoreCase);
    }
    static bool IsDescendantPath(string ParentPath, string LocalPath)
    {
        return !string.IsNullOrWhiteSpace(ParentPath)
            && !string.IsNullOrWhiteSpace(LocalPath)
            && LocalPath.StartsWith(ParentPath + "/", StringComparison.Ordinal);
    }
    static string ExistingPath(LocalObservedSnapshotRecord LocalObservation, BaseSnapshotRecord BaseSnapshot)
    {
        if (LocalObservation != null && !string.IsNullOrWhiteSpace(LocalObservation.RelativePath))
            return LocalObservation.RelativePath;

        return BaseSnapshot?.LocalRelativePath;
    }
    static string ReplacePathPrefix(string LocalPath, string OldPrefix, string NewPrefix)
    {
        string Suffix = LocalPath[OldPrefix.Length..];

        if (string.IsNullOrWhiteSpace(NewPrefix))
            return Suffix.StartsWith("/", StringComparison.Ordinal) ? Suffix[1..] : Suffix;

        return NewPrefix + Suffix;
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
    static bool IsInactiveRemoteObservation(RemoteObservedSnapshotRecord Observation)
    {
        return Observation != null
            && (Observation.Removed || Observation.Trashed == true || !Observation.ExistsFlag);
    }
    static bool ShouldReplaceTrackedRemoteItemId(SyncExecutionResult Result, TrackedItemRecord TrackedItem)
    {
        return Result?.RemoteItem != null
            && TrackedItem != null
            && !string.Equals(TrackedItem.RemoteItemId, Result.RemoteItem.Id, StringComparison.Ordinal)
            && Result.Request?.Decision?.DecisionKind == SyncPlanDecisionKind.UploadToRemote
            && IsInactiveRemoteObservation(Result.Request.RemoteObservation);
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
    static LocalObservedSnapshotRecord CreateMovedLocalObservation(LocalObservedSnapshotRecord Source, string LocalRelativePath, DateTime ObservedTime)
    {
        return new LocalObservedSnapshotRecord()
        {
            TrackedItemId = Source.TrackedItemId,
            ExistsFlag = Source.ExistsFlag,
            RelativePath = LocalRelativePath,
            Name = LocalName(LocalRelativePath),
            ParentRelativePath = LocalParentRelativePath(LocalRelativePath),
            ItemType = Source.ItemType,
            Size = Source.Size,
            ModifiedTime = Source.ModifiedTime,
            ContentHash = Source.ContentHash,
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

        if (TrackedItem != null && (string.IsNullOrWhiteSpace(TrackedItem.RemoteItemId) || ShouldReplaceTrackedRemoteItemId(Result, TrackedItem)))
        {
            TrackedItem.RemoteItemId = Result.RemoteItem.Id;
            fStore.UpdateTrackedItem(TrackedItem);
            Result.Request.TrackedItem = TrackedItem;
        }

        RemoteObservedSnapshotRecord Observation = RemoteObservationMapper.FromStorageItem(Result.RemoteItem, ItemId, ObservedTime);
        fStore.UpsertRemoteObservation(Observation);
        Result.Request.RemoteObservation = Observation;
    }
    List<string> ApplyLocalPathObservation(SyncExecutionResult Result, DateTime ObservedTime)
    {
        List<string> AffectedTrackedItemIds = new();

        if (string.IsNullOrWhiteSpace(Result.LocalRelativePath))
            return AffectedTrackedItemIds;

        string ItemId = TrackedItemId(Result);
        TrackedItemRecord TrackedItem = Result.Request.TrackedItem ?? fStore.GetTrackedItem(ItemId);
        string OldFolderPath = Result.Request.BaseSnapshot?.LocalRelativePath ?? Result.Request.LocalObservation?.RelativePath;

        if (TrackedItem != null && !string.Equals(TrackedItem.LocalKey, Result.LocalRelativePath, StringComparison.Ordinal))
        {
            TrackedItem.LocalKey = Result.LocalRelativePath;
            fStore.UpdateTrackedItem(TrackedItem);
            Result.Request.TrackedItem = TrackedItem;
        }

        LocalObservedSnapshotRecord Observation = CreateLocalObservationFromExecution(Result, ObservedTime);
        fStore.UpsertLocalObservation(Observation);
        Result.Request.LocalObservation = Observation;

        AffectedTrackedItemIds.Add(ItemId);

        if ((!IsRemoteNamespaceApply(Result) && !IsLocalNamespaceApply(Result)) || !IsFolder(Result))
            return AffectedTrackedItemIds;

        string NewFolderPath = Result.LocalRelativePath;

        if (string.IsNullOrWhiteSpace(OldFolderPath) || string.IsNullOrWhiteSpace(NewFolderPath) || string.Equals(OldFolderPath, NewFolderPath, StringComparison.Ordinal))
            return AffectedTrackedItemIds;

        foreach (TrackedItemRecord Item in fStore.GetTrackedItems(Result.Request.TrackedItem.SyncRootId))
        {
            if (string.Equals(Item.Id, ItemId, StringComparison.Ordinal))
                continue;

            LocalObservedSnapshotRecord LocalObservation = fStore.GetLocalObservation(Item.Id);
            BaseSnapshotRecord BaseSnapshot = fStore.GetBaseSnapshot(Item.Id);
            string ExistingPath = LocalObservation?.RelativePath;
            string CommittedPath = BaseSnapshot?.LocalRelativePath;

            if (LocalObservation == null || !LocalObservation.ExistsFlag)
                continue;

            string NewLocalPath;
            if (IsDescendantPath(OldFolderPath, ExistingPath))
                NewLocalPath = ReplacePathPrefix(ExistingPath, OldFolderPath, NewFolderPath);
            else if (IsDescendantPath(OldFolderPath, CommittedPath))
                NewLocalPath = IsDescendantPath(NewFolderPath, ExistingPath)
                    ? ExistingPath
                    : ReplacePathPrefix(CommittedPath, OldFolderPath, NewFolderPath);
            else
                continue;

            Item.LocalKey = NewLocalPath;
            fStore.UpdateTrackedItem(Item);
            fStore.UpsertLocalObservation(CreateMovedLocalObservation(LocalObservation, NewLocalPath, ObservedTime));
            AffectedTrackedItemIds.Add(Item.Id);
        }

        return AffectedTrackedItemIds;
    }
    void ApplyLocalMissingObservation(SyncExecutionResult Result, DateTime ObservedTime)
    {
        if (!IsRemoteDeletePropagation(Result))
            return;

        LocalObservedSnapshotRecord Observation = LocalObservationMapper.Missing(TrackedItemId(Result), ObservedTime, "execution");
        fStore.UpsertLocalObservation(Observation);
        Result.Request.LocalObservation = Observation;
    }
    RemoteObservedSnapshotRecord CreateImplicitRemoteDeleteObservation(TrackedItemRecord TrackedItem, DateTime ObservedTime, bool Removed)
    {
        RemoteObservedSnapshotRecord ExistingObservation = fStore.GetRemoteObservation(TrackedItem.Id);

        if (!Removed && ExistingObservation != null)
        {
            ExistingObservation.Trashed = true;
            ExistingObservation.ObservedTime = ObservedTime;
            return ExistingObservation;
        }

        return new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItem.Id,
            RemoteItemId = TrackedItem.RemoteItemId,
            ExistsFlag = false,
            Removed = Removed,
            ObservedTime = ObservedTime,
        };
    }
    List<string> ApplyRemoteFolderDeleteDescendantObservations(SyncExecutionResult Result, DateTime ObservedTime)
    {
        List<string> AffectedTrackedItemIds = new();

        if (!IsRemoteDeletePropagation(Result) || !IsFolder(Result))
            return AffectedTrackedItemIds;

        string FolderPath = Result.Request.BaseSnapshot?.LocalRelativePath ?? Result.Request.LocalObservation?.RelativePath;
        string SyncRootId = Result.Request.TrackedItem?.SyncRootId;

        if (string.IsNullOrWhiteSpace(FolderPath) || string.IsNullOrWhiteSpace(SyncRootId))
            return AffectedTrackedItemIds;

        bool Removed = Result.Request.RemoteObservation == null
            || !Result.Request.RemoteObservation.ExistsFlag
            || Result.Request.RemoteObservation.Removed;

        foreach (TrackedItemRecord Item in fStore.GetTrackedItems(SyncRootId))
        {
            if (string.Equals(Item.Id, TrackedItemId(Result), StringComparison.Ordinal))
                continue;

            LocalObservedSnapshotRecord LocalObservation = fStore.GetLocalObservation(Item.Id);
            BaseSnapshotRecord BaseSnapshot = fStore.GetBaseSnapshot(Item.Id);
            string ItemPath = ExistingPath(LocalObservation, BaseSnapshot);

            if (!IsDescendantPath(FolderPath, ItemPath))
                continue;

            fStore.UpsertLocalObservation(LocalObservationMapper.Missing(Item.Id, ObservedTime, "execution"));
            fStore.UpsertRemoteObservation(CreateImplicitRemoteDeleteObservation(Item, ObservedTime, Removed));
            AffectedTrackedItemIds.Add(Item.Id);
        }

        return AffectedTrackedItemIds;
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
        HashSet<string> UsedNamespaceTrackedItemIds = new();
        List<(string OldPrefix, string NewPrefix)> NamespacePrefixMaps = new();
        List<LocalScanItem> UnmatchedItems = new();

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
                    UnmatchedItems.Add(Item);
                    continue;
                }
            }

            Result.Observations.Add(LocalObservationMapper.FromScanItem(Item, TrackedItem.Id, ObservedTime, ScanId));
        }

        foreach (LocalScanItem Item in UnmatchedItems.OrderBy(Item => LocalPathDepth(LocalKey(Item))))
        {
            string Key = LocalKey(Item);
            TrackedItemRecord TrackedItem = FindLocalNamespaceDescendantCandidate(
                Item,
                TrackedItemsByLocalKey,
                NamespacePrefixMaps,
                ObservedLocalKeys,
                UsedNamespaceTrackedItemIds)
                ?? FindLocalNamespaceCandidate(SyncRootId, Item, ObservedLocalKeys, UsedNamespaceTrackedItemIds);

            if (TrackedItem != null)
            {
                string OldKey = TrackedItem.LocalKey;
                TrackedItemsByLocalKey.Remove(TrackedItem.LocalKey);
                TrackedItem.LocalKey = Key;
                TrackedItemsByLocalKey[Key] = TrackedItem;
                UsedNamespaceTrackedItemIds.Add(TrackedItem.Id);
                fStore.UpdateTrackedItem(TrackedItem);

                if (string.Equals(Item.ItemType, "Folder", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(OldKey) && !string.Equals(OldKey, Key, StringComparison.Ordinal))
                    NamespacePrefixMaps.Add((OldKey, Key));
            }
            else
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
                else if (Change.Removed)
                    continue;
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
        IReadOnlyList<BaseSnapshotRecord> CommittedBaseSnapshots = SavePlanningSideEffects(SyncRootId, Decisions, CommittedTime);

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
                TrackedItemIds.AddRange(ApplyLocalPathObservation(ExecutionResult, CommittedTime));
                ApplyLocalMissingObservation(ExecutionResult, CommittedTime);
                TrackedItemIds.AddRange(ApplyRemoteFolderDeleteDescendantObservations(ExecutionResult, CommittedTime));
            }

            if (CanCommitExecutionResult(ExecutionResult) && HasVerifiedCommitObservations(ExecutionResult))
            {
                if (!TrackedItemIds.Contains(TrackedItemId(ExecutionResult)))
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

        SyncExecutionApplyResult Result = new();

        foreach (SyncExecutionRequest Request in OrderExecutionRequests(Requests))
        {
            CancellationToken.ThrowIfCancellationRequested();

            SyncExecutionRequest RefreshedRequest = RefreshExecutionRequest(Request);
            IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync([RefreshedRequest], CancellationToken);
            AddExecutionApplyResult(Result, ApplyExecutionResults(Results, CommittedTime));
        }

        return Result;
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

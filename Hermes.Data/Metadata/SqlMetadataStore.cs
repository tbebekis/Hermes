// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Persists synchronization metadata records in a SQL database.
/// </summary>
public class SqlMetadataStore
{
    // ● fields

    readonly SqlStore fStore;

    // ● value readers

    static object DbValue(object Value) => Value ?? DBNull.Value;
    static string ReadString(DataRow Row, string FieldName)
    {
        object Value = Row[FieldName];
        return Value == DBNull.Value ? null : Convert.ToString(Value);
    }
    static bool ReadBool(DataRow Row, string FieldName) => Convert.ToBoolean(Row[FieldName]);
    static bool? ReadNullableBool(DataRow Row, string FieldName)
    {
        object Value = Row[FieldName];
        return Value == DBNull.Value ? null : Convert.ToBoolean(Value);
    }
    static long? ReadNullableLong(DataRow Row, string FieldName)
    {
        object Value = Row[FieldName];
        return Value == DBNull.Value ? null : Convert.ToInt64(Value);
    }
    static DateTime ReadDateTime(DataRow Row, string FieldName) => Convert.ToDateTime(Row[FieldName]);
    static DateTime? ReadNullableDateTime(DataRow Row, string FieldName)
    {
        object Value = Row[FieldName];
        return Value == DBNull.Value ? null : Convert.ToDateTime(Value);
    }
    static T ReadEnum<T>(DataRow Row, string FieldName) where T : struct
    {
        return Enum.Parse<T>(ReadString(Row, FieldName));
    }
    static DataRow FirstRow(MemTable Table) => Table.Rows.Count == 0 ? null : Table.Rows[0];
    static DataRow SingleOptionalRow(MemTable Table, string Description)
    {
        if (Table.Rows.Count == 0)
            return null;

        if (Table.Rows.Count > 1)
            throw new TripousDataException($"Multiple rows found for {Description}.");

        return Table.Rows[0];
    }

    // ● sql execution

    int ExecuteUpsert(DbTransaction Transaction, string UpdateSql, string InsertSql, Dictionary<string, object> Params)
    {
        int Count = fStore.ExecSql(Transaction, UpdateSql, Params);
        if (Count == 0)
            Count = fStore.ExecSql(Transaction, InsertSql, Params);

        return Count;
    }
    void ExecuteUpsert(string UpdateSql, string InsertSql, Dictionary<string, object> Params)
    {
        using SqlTransactionContext Context = fStore.BeginTransactionContext();
        ExecuteUpsert(Context.Transaction, UpdateSql, InsertSql, Params);
        Context.Commit();
    }
    void ExecuteInsertTrackedItem(DbTransaction Transaction, TrackedItemRecord Record)
    {
        fStore.ExecSql(Transaction, Sql.InsertTrackedItem, ToParams(Record));
    }
    void StoreRemoteObservation(DbTransaction Transaction, RemoteObservedSnapshotRecord Observation)
    {
        ExecuteUpsert(Transaction, Sql.UpdateRemoteObservation, Sql.InsertRemoteObservation, ToParams(Observation));
    }
    void StoreBaseSnapshot(DbTransaction Transaction, BaseSnapshotRecord Record)
    {
        ExecuteUpsert(Transaction, Sql.UpdateBaseSnapshot, Sql.InsertBaseSnapshot, ToParams(Record));
    }
    SyncConflictRecord GetOpenConflict(DbTransaction Transaction, string TrackedItemId)
    {
        DataRow Row = FirstRow(fStore.Select(
            Transaction,
            "select * from SYNC_CONFLICT where TrackedItemId = :TrackedItemId and State = :State",
            new Dictionary<string, object>()
            {
                ["TrackedItemId"] = TrackedItemId,
                ["State"] = SyncConflictState.Open.ToString(),
            }));
        return Row == null ? null : ToSyncConflict(Row);
    }
    void StoreOpenConflict(DbTransaction Transaction, SyncConflictRecord Record)
    {
        SyncConflictRecord Existing = GetOpenConflict(Transaction, Record.TrackedItemId);
        if (Existing == null)
        {
            if (string.IsNullOrWhiteSpace(Record.Id))
                Record.Id = Sys.GenId();

            if (Record.FirstObservedTime == default)
                Record.FirstObservedTime = Record.LastObservedTime;
        }
        else
        {
            Record.Id = Existing.Id;
            Record.FirstObservedTime = Existing.FirstObservedTime;
        }

        Record.State = SyncConflictState.Open;
        Record.ResolvedTime = null;
        ExecuteUpsert(Transaction, Sql.UpdateSyncConflict, Sql.InsertSyncConflict, ToParams(Record));
    }
    int ResolveOpenConflict(DbTransaction Transaction, string TrackedItemId, DateTime ResolvedTime)
    {
        return fStore.ExecSql(
            Transaction,
            Sql.ResolveSyncConflict,
            new Dictionary<string, object>()
            {
                ["TrackedItemId"] = TrackedItemId,
                ["ResolvedTime"] = ResolvedTime,
                ["State"] = SyncConflictState.Resolved.ToString(),
                ["OpenState"] = SyncConflictState.Open.ToString(),
            });
    }
    void StoreRemoteObservations(DbTransaction Transaction, IEnumerable<RemoteObservedSnapshotRecord> Observations)
    {
        foreach (RemoteObservedSnapshotRecord Observation in Observations)
            StoreRemoteObservation(Transaction, Observation);
    }
    void StoreRemoteChangeImportResult(DbTransaction Transaction, RemoteChangeImportResult Result)
    {
        foreach (TrackedItemRecord TrackedItem in Result.CreatedTrackedItems)
            ExecuteInsertTrackedItem(Transaction, TrackedItem);

        StoreRemoteObservations(Transaction, Result.Observations);
    }
    void StoreRemoteBootstrapResult(DbTransaction Transaction, RemoteBootstrapResult Result)
    {
        foreach (TrackedItemRecord TrackedItem in Result.CreatedTrackedItems)
            ExecuteInsertTrackedItem(Transaction, TrackedItem);

        foreach (TrackedItemRecord TrackedItem in Result.AdoptedTrackedItems)
            fStore.ExecSql(Transaction, Sql.UpdateTrackedItem, ToParams(TrackedItem));

        StoreRemoteObservations(Transaction, Result.Observations);

        foreach (BaseSnapshotRecord BaseSnapshot in Result.CommittedBaseSnapshots)
            StoreBaseSnapshot(Transaction, BaseSnapshot);
    }

    // ● parameter mapping

    static Dictionary<string, object> ToParams(SyncRootRecord Record) => new()
    {
        ["Id"] = Record.Id,
        ["ProviderName"] = Record.ProviderName,
        ["ConnectionId"] = DbValue(Record.ConnectionId),
        ["LocalRootPath"] = Record.LocalRootPath,
        ["RemoteRootItemId"] = DbValue(Record.RemoteRootItemId),
        ["IsEnabled"] = Record.IsEnabled,
        ["CreatedTime"] = Record.CreatedTime,
    };
    static Dictionary<string, object> ToParams(TrackedItemRecord Record) => new()
    {
        ["Id"] = Record.Id,
        ["SyncRootId"] = Record.SyncRootId,
        ["RemoteItemId"] = DbValue(Record.RemoteItemId),
        ["LocalKey"] = DbValue(Record.LocalKey),
        ["ItemType"] = Record.ItemType,
    };
    static Dictionary<string, object> ToParams(BaseSnapshotRecord Record) => new()
    {
        ["TrackedItemId"] = Record.TrackedItemId,
        ["ExistsFlag"] = Record.ExistsFlag,
        ["ItemType"] = DbValue(Record.ItemType),
        ["Name"] = DbValue(Record.Name),
        ["LocalRelativePath"] = DbValue(Record.LocalRelativePath),
        ["RemoteParentId"] = DbValue(Record.RemoteParentId),
        ["Size"] = DbValue(Record.Size),
        ["ContentHash"] = DbValue(Record.ContentHash),
        ["CreatedTime"] = DbValue(Record.CreatedTime),
        ["ModifiedTime"] = DbValue(Record.ModifiedTime),
        ["ProviderVersion"] = DbValue(Record.ProviderVersion),
        ["Trashed"] = DbValue(Record.Trashed),
        ["CommittedTime"] = Record.CommittedTime,
    };
    static Dictionary<string, object> ToParams(LocalObservedSnapshotRecord Record) => new()
    {
        ["TrackedItemId"] = Record.TrackedItemId,
        ["ExistsFlag"] = Record.ExistsFlag,
        ["RelativePath"] = DbValue(Record.RelativePath),
        ["Name"] = DbValue(Record.Name),
        ["ParentRelativePath"] = DbValue(Record.ParentRelativePath),
        ["ItemType"] = DbValue(Record.ItemType),
        ["Size"] = DbValue(Record.Size),
        ["ModifiedTime"] = DbValue(Record.ModifiedTime),
        ["ContentHash"] = DbValue(Record.ContentHash),
        ["ObservedTime"] = Record.ObservedTime,
        ["ScanId"] = DbValue(Record.ScanId),
    };
    static Dictionary<string, object> ToParams(RemoteObservedSnapshotRecord Record) => new()
    {
        ["TrackedItemId"] = Record.TrackedItemId,
        ["RemoteItemId"] = Record.RemoteItemId,
        ["ExistsFlag"] = Record.ExistsFlag,
        ["Removed"] = Record.Removed,
        ["Name"] = DbValue(Record.Name),
        ["RemoteParentId"] = DbValue(Record.RemoteParentId),
        ["ItemType"] = DbValue(Record.ItemType),
        ["MimeType"] = DbValue(Record.MimeType),
        ["Size"] = DbValue(Record.Size),
        ["ContentHash"] = DbValue(Record.ContentHash),
        ["CreatedTime"] = DbValue(Record.CreatedTime),
        ["ModifiedTime"] = DbValue(Record.ModifiedTime),
        ["ProviderVersion"] = DbValue(Record.ProviderVersion),
        ["Trashed"] = DbValue(Record.Trashed),
        ["ProviderChangeTime"] = DbValue(Record.ProviderChangeTime),
        ["ObservedTime"] = Record.ObservedTime,
    };
    static Dictionary<string, object> ToParams(RemoteCheckpointRecord Record) => new()
    {
        ["SyncRootId"] = Record.SyncRootId,
        ["ProviderName"] = Record.ProviderName,
        ["ConnectionId"] = DbValue(Record.ConnectionId),
        ["StartPageToken"] = DbValue(Record.StartPageToken),
        ["UpdatedTime"] = DbValue(Record.UpdatedTime),
    };
    static Dictionary<string, object> ToParams(SyncConflictRecord Record) => new()
    {
        ["Id"] = Record.Id,
        ["SyncRootId"] = Record.SyncRootId,
        ["TrackedItemId"] = Record.TrackedItemId,
        ["DiffKind"] = Record.DiffKind.ToString(),
        ["DecisionKind"] = Record.DecisionKind.ToString(),
        ["State"] = Record.State.ToString(),
        ["Message"] = DbValue(Record.Message),
        ["FirstObservedTime"] = Record.FirstObservedTime,
        ["LastObservedTime"] = Record.LastObservedTime,
        ["ResolvedTime"] = DbValue(Record.ResolvedTime),
    };

    // ● row mapping

    static SyncRootRecord ToSyncRoot(DataRow Row) => new()
    {
        Id = ReadString(Row, "Id"),
        ProviderName = ReadString(Row, "ProviderName"),
        ConnectionId = ReadString(Row, "ConnectionId"),
        LocalRootPath = ReadString(Row, "LocalRootPath"),
        RemoteRootItemId = ReadString(Row, "RemoteRootItemId"),
        IsEnabled = ReadBool(Row, "IsEnabled"),
        CreatedTime = ReadDateTime(Row, "CreatedTime"),
    };
    static TrackedItemRecord ToTrackedItem(DataRow Row) => new()
    {
        Id = ReadString(Row, "Id"),
        SyncRootId = ReadString(Row, "SyncRootId"),
        RemoteItemId = ReadString(Row, "RemoteItemId"),
        LocalKey = ReadString(Row, "LocalKey"),
        ItemType = ReadString(Row, "ItemType"),
    };
    static BaseSnapshotRecord ToBaseSnapshot(DataRow Row) => new()
    {
        TrackedItemId = ReadString(Row, "TrackedItemId"),
        ExistsFlag = ReadBool(Row, "ExistsFlag"),
        ItemType = ReadString(Row, "ItemType"),
        Name = ReadString(Row, "Name"),
        LocalRelativePath = ReadString(Row, "LocalRelativePath"),
        RemoteParentId = ReadString(Row, "RemoteParentId"),
        Size = ReadNullableLong(Row, "Size"),
        ContentHash = ReadString(Row, "ContentHash"),
        CreatedTime = ReadNullableDateTime(Row, "CreatedTime"),
        ModifiedTime = ReadNullableDateTime(Row, "ModifiedTime"),
        ProviderVersion = ReadNullableLong(Row, "ProviderVersion"),
        Trashed = ReadNullableBool(Row, "Trashed"),
        CommittedTime = ReadDateTime(Row, "CommittedTime"),
    };
    static LocalObservedSnapshotRecord ToLocalObservation(DataRow Row) => new()
    {
        TrackedItemId = ReadString(Row, "TrackedItemId"),
        ExistsFlag = ReadBool(Row, "ExistsFlag"),
        RelativePath = ReadString(Row, "RelativePath"),
        Name = ReadString(Row, "Name"),
        ParentRelativePath = ReadString(Row, "ParentRelativePath"),
        ItemType = ReadString(Row, "ItemType"),
        Size = ReadNullableLong(Row, "Size"),
        ModifiedTime = ReadNullableDateTime(Row, "ModifiedTime"),
        ContentHash = ReadString(Row, "ContentHash"),
        ObservedTime = ReadDateTime(Row, "ObservedTime"),
        ScanId = ReadString(Row, "ScanId"),
    };
    static RemoteObservedSnapshotRecord ToRemoteObservation(DataRow Row) => new()
    {
        TrackedItemId = ReadString(Row, "TrackedItemId"),
        RemoteItemId = ReadString(Row, "RemoteItemId"),
        ExistsFlag = ReadBool(Row, "ExistsFlag"),
        Removed = ReadBool(Row, "Removed"),
        Name = ReadString(Row, "Name"),
        RemoteParentId = ReadString(Row, "RemoteParentId"),
        ItemType = ReadString(Row, "ItemType"),
        MimeType = ReadString(Row, "MimeType"),
        Size = ReadNullableLong(Row, "Size"),
        ContentHash = ReadString(Row, "ContentHash"),
        CreatedTime = ReadNullableDateTime(Row, "CreatedTime"),
        ModifiedTime = ReadNullableDateTime(Row, "ModifiedTime"),
        ProviderVersion = ReadNullableLong(Row, "ProviderVersion"),
        Trashed = ReadNullableBool(Row, "Trashed"),
        ProviderChangeTime = ReadNullableDateTime(Row, "ProviderChangeTime"),
        ObservedTime = ReadDateTime(Row, "ObservedTime"),
    };
    static RemoteCheckpointRecord ToRemoteCheckpoint(DataRow Row) => new()
    {
        SyncRootId = ReadString(Row, "SyncRootId"),
        ProviderName = ReadString(Row, "ProviderName"),
        ConnectionId = ReadString(Row, "ConnectionId"),
        StartPageToken = ReadString(Row, "StartPageToken"),
        UpdatedTime = ReadNullableDateTime(Row, "UpdatedTime"),
    };
    static SyncConflictRecord ToSyncConflict(DataRow Row) => new()
    {
        Id = ReadString(Row, "Id"),
        SyncRootId = ReadString(Row, "SyncRootId"),
        TrackedItemId = ReadString(Row, "TrackedItemId"),
        DiffKind = ReadEnum<SyncDiffKind>(Row, "DiffKind"),
        DecisionKind = ReadEnum<SyncPlanDecisionKind>(Row, "DecisionKind"),
        State = ReadEnum<SyncConflictState>(Row, "State"),
        Message = ReadString(Row, "Message"),
        FirstObservedTime = ReadDateTime(Row, "FirstObservedTime"),
        LastObservedTime = ReadDateTime(Row, "LastObservedTime"),
        ResolvedTime = ReadNullableDateTime(Row, "ResolvedTime"),
    };

    // ● namespace helpers

    static string NamespaceKey(RemoteObservedSnapshotRecord Record)
    {
        return $"{Record.RemoteParentId ?? string.Empty}\u001F{Record.Name ?? string.Empty}";
    }
    static bool IsActiveRemoteNamespaceItem(RemoteObservedSnapshotRecord Record)
    {
        return Record != null
            && Record.ExistsFlag
            && !Record.Removed
            && Record.Trashed != true
            && !string.IsNullOrWhiteSpace(Record.Name);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMetadataStore"/> class.
    /// </summary>
    public SqlMetadataStore(SqlStore Store)
    {
        fStore = Store ?? throw new ArgumentNullException(nameof(Store));
    }

    // ● sync roots

    /// <summary>
    /// Inserts a sync root.
    /// </summary>
    public void InsertSyncRoot(SyncRootRecord Record)
    {
        string SqlText = @"
insert into SYNC_ROOT
    (Id, ProviderName, ConnectionId, LocalRootPath, RemoteRootItemId, IsEnabled, CreatedTime)
values
    (:Id, :ProviderName, :ConnectionId, :LocalRootPath, :RemoteRootItemId, :IsEnabled, :CreatedTime)";
        fStore.ExecSql(SqlText, ToParams(Record));
    }
    /// <summary>
    /// Updates a sync root.
    /// </summary>
    public void UpdateSyncRoot(SyncRootRecord Record)
    {
        string SqlText = @"
update SYNC_ROOT set
    ProviderName = :ProviderName,
    ConnectionId = :ConnectionId,
    LocalRootPath = :LocalRootPath,
    RemoteRootItemId = :RemoteRootItemId,
    IsEnabled = :IsEnabled,
    CreatedTime = :CreatedTime
where Id = :Id";
        fStore.ExecSql(SqlText, ToParams(Record));
    }
    /// <summary>
    /// Returns a sync root by id.
    /// </summary>
    public SyncRootRecord GetSyncRoot(string Id)
    {
        DataRow Row = FirstRow(fStore.Select("select * from SYNC_ROOT where Id = :Id", new Dictionary<string, object>() { ["Id"] = Id }));
        return Row == null ? null : ToSyncRoot(Row);
    }

    // ● tracked items

    /// <summary>
    /// Inserts a tracked item.
    /// </summary>
    public void InsertTrackedItem(TrackedItemRecord Record)
    {
        fStore.ExecSql(Sql.InsertTrackedItem, ToParams(Record));
    }
    /// <summary>
    /// Updates an existing tracked item.
    /// </summary>
    public void UpdateTrackedItem(TrackedItemRecord Record)
    {
        fStore.ExecSql(Sql.UpdateTrackedItem, ToParams(Record));
    }
    /// <summary>
    /// Returns a tracked item by id.
    /// </summary>
    public TrackedItemRecord GetTrackedItem(string Id)
    {
        DataRow Row = FirstRow(fStore.Select("select * from TRACKED_ITEM where Id = :Id", new Dictionary<string, object>() { ["Id"] = Id }));
        return Row == null ? null : ToTrackedItem(Row);
    }
    /// <summary>
    /// Returns a tracked item by remote item id inside a sync root.
    /// </summary>
    public TrackedItemRecord GetTrackedItemByRemoteId(string SyncRootId, string RemoteItemId)
    {
        MemTable Table = fStore.Select(
            "select * from TRACKED_ITEM where SyncRootId = :SyncRootId and RemoteItemId = :RemoteItemId",
            new Dictionary<string, object>()
            {
                ["SyncRootId"] = SyncRootId,
                ["RemoteItemId"] = RemoteItemId,
            });
        DataRow Row = SingleOptionalRow(Table, $"remote item id {RemoteItemId}");
        return Row == null ? null : ToTrackedItem(Row);
    }
    /// <summary>
    /// Returns a tracked item by local key inside a sync root.
    /// </summary>
    public TrackedItemRecord GetTrackedItemByLocalKey(string SyncRootId, string LocalKey)
    {
        MemTable Table = fStore.Select(
            "select * from TRACKED_ITEM where SyncRootId = :SyncRootId and LocalKey = :LocalKey",
            new Dictionary<string, object>()
            {
                ["SyncRootId"] = SyncRootId,
                ["LocalKey"] = LocalKey,
            });
        DataRow Row = SingleOptionalRow(Table, $"local key {LocalKey}");
        return Row == null ? null : ToTrackedItem(Row);
    }
    /// <summary>
    /// Returns tracked items for a sync root.
    /// </summary>
    public IReadOnlyList<TrackedItemRecord> GetTrackedItems(string SyncRootId)
    {
        MemTable Table = fStore.Select("select * from TRACKED_ITEM where SyncRootId = :SyncRootId order by Id", new Dictionary<string, object>() { ["SyncRootId"] = SyncRootId });
        List<TrackedItemRecord> Result = new();

        foreach (DataRow Row in Table.Rows)
            Result.Add(ToTrackedItem(Row));

        return Result;
    }

    // ● base snapshots

    /// <summary>
    /// Inserts or updates a base snapshot.
    /// </summary>
    public void UpsertBaseSnapshot(BaseSnapshotRecord Record)
    {
        ExecuteUpsert(Sql.UpdateBaseSnapshot, Sql.InsertBaseSnapshot, ToParams(Record));
    }
    /// <summary>
    /// Returns a base snapshot by tracked item id.
    /// </summary>
    public BaseSnapshotRecord GetBaseSnapshot(string TrackedItemId)
    {
        DataRow Row = FirstRow(fStore.Select("select * from BASE_SNAPSHOT where TrackedItemId = :TrackedItemId", new Dictionary<string, object>() { ["TrackedItemId"] = TrackedItemId }));
        return Row == null ? null : ToBaseSnapshot(Row);
    }
    /// <summary>
    /// Commits the current verified local and remote observations as the base snapshot.
    /// </summary>
    public BaseSnapshotRecord CommitBaseSnapshotFromObservations(string TrackedItemId, DateTime CommittedTime)
    {
        Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemId));

        LocalObservedSnapshotRecord LocalObservation = GetLocalObservation(TrackedItemId);
        RemoteObservedSnapshotRecord RemoteObservation = GetRemoteObservation(TrackedItemId);
        BaseSnapshotRecord BaseSnapshot = BaseSnapshotMapper.FromVerifiedObservations(LocalObservation, RemoteObservation, CommittedTime);

        UpsertBaseSnapshot(BaseSnapshot);

        return BaseSnapshot;
    }
    /// <summary>
    /// Commits current verified local and remote observations as base snapshots in one transaction.
    /// </summary>
    public IReadOnlyList<BaseSnapshotRecord> CommitBaseSnapshotsFromObservations(IEnumerable<string> TrackedItemIds, DateTime CommittedTime)
    {
        Guard.NotNull(TrackedItemIds, nameof(TrackedItemIds));

        List<BaseSnapshotRecord> BaseSnapshots = new();

        foreach (string TrackedItemId in TrackedItemIds)
        {
            Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemIds));

            LocalObservedSnapshotRecord LocalObservation = GetLocalObservation(TrackedItemId);
            RemoteObservedSnapshotRecord RemoteObservation = GetRemoteObservation(TrackedItemId);
            BaseSnapshots.Add(BaseSnapshotMapper.FromVerifiedObservations(LocalObservation, RemoteObservation, CommittedTime));
        }

        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        foreach (BaseSnapshotRecord BaseSnapshot in BaseSnapshots)
            StoreBaseSnapshot(Context.Transaction, BaseSnapshot);

        Context.Commit();

        return BaseSnapshots;
    }
    /// <summary>
    /// Applies metadata-only planning side effects in one transaction.
    /// </summary>
    public IReadOnlyList<BaseSnapshotRecord> SavePlanningSideEffects(
        IEnumerable<string> CommitBaseTrackedItemIds,
        IEnumerable<SyncConflictRecord> OpenConflicts,
        IEnumerable<string> ResolvedConflictTrackedItemIds,
        DateTime CommittedTime)
    {
        Guard.NotNull(CommitBaseTrackedItemIds, nameof(CommitBaseTrackedItemIds));
        Guard.NotNull(OpenConflicts, nameof(OpenConflicts));
        Guard.NotNull(ResolvedConflictTrackedItemIds, nameof(ResolvedConflictTrackedItemIds));

        List<BaseSnapshotRecord> BaseSnapshots = new();

        foreach (string TrackedItemId in CommitBaseTrackedItemIds)
        {
            Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(CommitBaseTrackedItemIds));

            LocalObservedSnapshotRecord LocalObservation = GetLocalObservation(TrackedItemId);
            RemoteObservedSnapshotRecord RemoteObservation = GetRemoteObservation(TrackedItemId);
            BaseSnapshots.Add(BaseSnapshotMapper.FromVerifiedObservations(LocalObservation, RemoteObservation, CommittedTime));
        }

        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        foreach (BaseSnapshotRecord BaseSnapshot in BaseSnapshots)
            StoreBaseSnapshot(Context.Transaction, BaseSnapshot);

        foreach (SyncConflictRecord Conflict in OpenConflicts)
            StoreOpenConflict(Context.Transaction, Conflict);

        foreach (string TrackedItemId in ResolvedConflictTrackedItemIds)
            ResolveOpenConflict(Context.Transaction, TrackedItemId, CommittedTime);

        Context.Commit();

        return BaseSnapshots;
    }
    // ● local observations

    /// <summary>
    /// Inserts or updates a local observation.
    /// </summary>
    public void UpsertLocalObservation(LocalObservedSnapshotRecord Record)
    {
        ExecuteUpsert(Sql.UpdateLocalObservation, Sql.InsertLocalObservation, ToParams(Record));
    }
    /// <summary>
    /// Inserts or updates local observations in one transaction.
    /// </summary>
    public void SaveLocalObservations(IEnumerable<LocalObservedSnapshotRecord> Observations)
    {
        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        foreach (LocalObservedSnapshotRecord Observation in Observations)
            ExecuteUpsert(Context.Transaction, Sql.UpdateLocalObservation, Sql.InsertLocalObservation, ToParams(Observation));

        Context.Commit();
    }
    /// <summary>
    /// Stores local scan import created tracked items and observations in one transaction.
    /// </summary>
    public void SaveLocalScanImportResult(LocalScanImportResult Result)
    {
        Guard.NotNull(Result, nameof(Result));

        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        foreach (TrackedItemRecord TrackedItem in Result.CreatedTrackedItems)
            ExecuteInsertTrackedItem(Context.Transaction, TrackedItem);

        foreach (LocalObservedSnapshotRecord Observation in Result.Observations)
            ExecuteUpsert(Context.Transaction, Sql.UpdateLocalObservation, Sql.InsertLocalObservation, ToParams(Observation));

        Context.Commit();
    }
    /// <summary>
    /// Returns a local observation by tracked item id.
    /// </summary>
    public LocalObservedSnapshotRecord GetLocalObservation(string TrackedItemId)
    {
        DataRow Row = FirstRow(fStore.Select("select * from LOCAL_OBSERVED_SNAPSHOT where TrackedItemId = :TrackedItemId", new Dictionary<string, object>() { ["TrackedItemId"] = TrackedItemId }));
        return Row == null ? null : ToLocalObservation(Row);
    }
    // ● remote observations

    /// <summary>
    /// Inserts or updates a remote observation.
    /// </summary>
    public void UpsertRemoteObservation(RemoteObservedSnapshotRecord Record)
    {
        ExecuteUpsert(Sql.UpdateRemoteObservation, Sql.InsertRemoteObservation, ToParams(Record));
    }
    /// <summary>
    /// Inserts or updates remote observations in one transaction.
    /// </summary>
    public void SaveRemoteObservations(IEnumerable<RemoteObservedSnapshotRecord> Observations)
    {
        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        StoreRemoteObservations(Context.Transaction, Observations);
        Context.Commit();
    }
    /// <summary>
    /// Returns a remote observation by tracked item id.
    /// </summary>
    public RemoteObservedSnapshotRecord GetRemoteObservation(string TrackedItemId)
    {
        DataRow Row = FirstRow(fStore.Select("select * from REMOTE_OBSERVED_SNAPSHOT where TrackedItemId = :TrackedItemId", new Dictionary<string, object>() { ["TrackedItemId"] = TrackedItemId }));
        return Row == null ? null : ToRemoteObservation(Row);
    }
    // ● remote checkpoints

    /// <summary>
    /// Inserts or updates a remote checkpoint.
    /// </summary>
    public void UpsertRemoteCheckpoint(RemoteCheckpointRecord Record)
    {
        ExecuteUpsert(Sql.UpdateRemoteCheckpoint, Sql.InsertRemoteCheckpoint, ToParams(Record));
    }
    /// <summary>
    /// Returns a remote checkpoint by sync root id.
    /// </summary>
    public RemoteCheckpointRecord GetRemoteCheckpoint(string SyncRootId)
    {
        DataRow Row = FirstRow(fStore.Select("select * from REMOTE_CHECKPOINT where SyncRootId = :SyncRootId", new Dictionary<string, object>() { ["SyncRootId"] = SyncRootId }));
        return Row == null ? null : ToRemoteCheckpoint(Row);
    }
    /// <summary>
    /// Stores remote observations and checkpoint advancement atomically.
    /// </summary>
    public void SaveRemoteObservationsWithCheckpoint(IEnumerable<RemoteObservedSnapshotRecord> Observations, RemoteCheckpointRecord Checkpoint)
    {
        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        StoreRemoteObservations(Context.Transaction, Observations);
        ExecuteUpsert(Context.Transaction, Sql.UpdateRemoteCheckpoint, Sql.InsertRemoteCheckpoint, ToParams(Checkpoint));
        Context.Commit();
    }
    /// <summary>
    /// Stores remote bootstrap results and checkpoint advancement atomically.
    /// </summary>
    public void SaveRemoteBootstrapResultWithCheckpoint(RemoteBootstrapResult Result, RemoteCheckpointRecord Checkpoint)
    {
        Guard.NotNull(Result, nameof(Result));
        Guard.NotNull(Checkpoint, nameof(Checkpoint));

        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        StoreRemoteBootstrapResult(Context.Transaction, Result);
        ExecuteUpsert(Context.Transaction, Sql.UpdateRemoteCheckpoint, Sql.InsertRemoteCheckpoint, ToParams(Checkpoint));
        Context.Commit();
    }
    /// <summary>
    /// Stores remote change import results and checkpoint advancement atomically.
    /// </summary>
    public void SaveRemoteChangeImportResultWithCheckpoint(RemoteChangeImportResult Result, RemoteCheckpointRecord Checkpoint)
    {
        Guard.NotNull(Result, nameof(Result));
        Guard.NotNull(Checkpoint, nameof(Checkpoint));

        using SqlTransactionContext Context = fStore.BeginTransactionContext();

        StoreRemoteChangeImportResult(Context.Transaction, Result);
        ExecuteUpsert(Context.Transaction, Sql.UpdateRemoteCheckpoint, Sql.InsertRemoteCheckpoint, ToParams(Checkpoint));
        Context.Commit();
    }

    // ● diff and planning

    /// <summary>
    /// Returns classifier input for a tracked item.
    /// </summary>
    public SyncDiffInput GetDiffInput(string TrackedItemId, bool NamespaceCollisionDetected = false)
    {
        return SyncItemStateMapper.CreateDiffInput(
            GetBaseSnapshot(TrackedItemId),
            GetLocalObservation(TrackedItemId),
            GetRemoteObservation(TrackedItemId),
            NamespaceCollisionDetected);
    }
    /// <summary>
    /// Finds remote namespace collisions for a sync root.
    /// </summary>
    public IReadOnlyList<NamespaceCollisionRecord> FindRemoteNamespaceCollisions(string SyncRootId)
    {
        Dictionary<string, NamespaceCollisionRecord> Map = new();

        foreach (TrackedItemRecord Item in GetTrackedItems(SyncRootId))
        {
            RemoteObservedSnapshotRecord Observation = GetRemoteObservation(Item.Id);
            if (!IsActiveRemoteNamespaceItem(Observation))
                continue;

            string Key = NamespaceKey(Observation);
            if (!Map.TryGetValue(Key, out NamespaceCollisionRecord Collision))
            {
                Collision = new NamespaceCollisionRecord()
                {
                    RemoteParentId = Observation.RemoteParentId,
                    Name = Observation.Name,
                };
                Map[Key] = Collision;
            }

            Collision.TrackedItemIds.Add(Item.Id);
        }

        return Map.Values
            .Where(Item => Item.TrackedItemIds.Count > 1)
            .OrderBy(Item => Item.RemoteParentId)
            .ThenBy(Item => Item.Name)
            .ToList();
    }

    // ● conflicts

    /// <summary>
    /// Inserts or updates an open conflict.
    /// </summary>
    public SyncConflictRecord UpsertOpenConflict(string SyncRootId, SyncPlanDecision Decision, string Message, DateTime ObservedTime)
    {
        Guard.NotNullOrWhiteSpace(SyncRootId, nameof(SyncRootId));
        Guard.NotNull(Decision, nameof(Decision));

        SyncConflictRecord Record = new()
        {
            SyncRootId = SyncRootId,
            TrackedItemId = Decision.TrackedItemId,
            DiffKind = Decision.DiffKind,
            DecisionKind = Decision.DecisionKind,
            State = SyncConflictState.Open,
            Message = Message ?? string.Empty,
            FirstObservedTime = ObservedTime,
            LastObservedTime = ObservedTime,
        };

        using SqlTransactionContext Context = fStore.BeginTransactionContext();
        StoreOpenConflict(Context.Transaction, Record);
        Context.Commit();

        return Record;
    }
    /// <summary>
    /// Resolves an open conflict for a tracked item.
    /// </summary>
    public bool ResolveOpenConflict(string TrackedItemId, DateTime ResolvedTime)
    {
        Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemId));

        using SqlTransactionContext Context = fStore.BeginTransactionContext();
        int Count = ResolveOpenConflict(Context.Transaction, TrackedItemId, ResolvedTime);
        Context.Commit();

        return Count > 0;
    }
    /// <summary>
    /// Returns an open conflict by tracked item id.
    /// </summary>
    public SyncConflictRecord GetOpenConflict(string TrackedItemId)
    {
        DataRow Row = FirstRow(fStore.Select(
            "select * from SYNC_CONFLICT where TrackedItemId = :TrackedItemId and State = :State",
            new Dictionary<string, object>()
            {
                ["TrackedItemId"] = TrackedItemId,
                ["State"] = SyncConflictState.Open.ToString(),
            }));
        return Row == null ? null : ToSyncConflict(Row);
    }
    /// <summary>
    /// Returns open conflicts for a sync root.
    /// </summary>
    public IReadOnlyList<SyncConflictRecord> GetOpenConflicts(string SyncRootId)
    {
        MemTable Table = fStore.Select(
            "select * from SYNC_CONFLICT where SyncRootId = :SyncRootId and State = :State order by TrackedItemId",
            new Dictionary<string, object>()
            {
                ["SyncRootId"] = SyncRootId,
                ["State"] = SyncConflictState.Open.ToString(),
            });
        List<SyncConflictRecord> Result = new();

        foreach (DataRow Row in Table.Rows)
            Result.Add(ToSyncConflict(Row));

        return Result;
    }
    /// <summary>
    /// Returns the number of open conflicts for a sync root.
    /// </summary>
    public int CountOpenConflicts(string SyncRootId)
    {
        return fStore.IntegerResult(
            "select count(*) from SYNC_CONFLICT where SyncRootId = :SyncRootId and State = :State",
            0,
            new Dictionary<string, object>()
            {
                ["SyncRootId"] = SyncRootId,
                ["State"] = SyncConflictState.Open.ToString(),
            });
    }

    // ● private types

    /// <summary>
    /// Contains SQL statements used by the metadata store.
    /// </summary>
    static class Sql
    {
        // ● fields

        public const string InsertTrackedItem = @"
insert into TRACKED_ITEM
    (Id, SyncRootId, RemoteItemId, LocalKey, ItemType)
values
    (:Id, :SyncRootId, :RemoteItemId, :LocalKey, :ItemType)";
        public const string UpdateTrackedItem = @"
update TRACKED_ITEM set
    SyncRootId = :SyncRootId,
    RemoteItemId = :RemoteItemId,
    LocalKey = :LocalKey,
    ItemType = :ItemType
where Id = :Id";
        public const string UpdateBaseSnapshot = @"
update BASE_SNAPSHOT set
    ExistsFlag = :ExistsFlag,
    ItemType = :ItemType,
    Name = :Name,
    LocalRelativePath = :LocalRelativePath,
    RemoteParentId = :RemoteParentId,
    Size = :Size,
    ContentHash = :ContentHash,
    CreatedTime = :CreatedTime,
    ModifiedTime = :ModifiedTime,
    ProviderVersion = :ProviderVersion,
    Trashed = :Trashed,
    CommittedTime = :CommittedTime
where TrackedItemId = :TrackedItemId";
        public const string InsertBaseSnapshot = @"
insert into BASE_SNAPSHOT
    (TrackedItemId, ExistsFlag, ItemType, Name, LocalRelativePath, RemoteParentId, Size, ContentHash, CreatedTime, ModifiedTime, ProviderVersion, Trashed, CommittedTime)
values
    (:TrackedItemId, :ExistsFlag, :ItemType, :Name, :LocalRelativePath, :RemoteParentId, :Size, :ContentHash, :CreatedTime, :ModifiedTime, :ProviderVersion, :Trashed, :CommittedTime)";
        public const string UpdateLocalObservation = @"
update LOCAL_OBSERVED_SNAPSHOT set
    ExistsFlag = :ExistsFlag,
    RelativePath = :RelativePath,
    Name = :Name,
    ParentRelativePath = :ParentRelativePath,
    ItemType = :ItemType,
    Size = :Size,
    ModifiedTime = :ModifiedTime,
    ContentHash = :ContentHash,
    ObservedTime = :ObservedTime,
    ScanId = :ScanId
where TrackedItemId = :TrackedItemId";
        public const string InsertLocalObservation = @"
insert into LOCAL_OBSERVED_SNAPSHOT
    (TrackedItemId, ExistsFlag, RelativePath, Name, ParentRelativePath, ItemType, Size, ModifiedTime, ContentHash, ObservedTime, ScanId)
values
    (:TrackedItemId, :ExistsFlag, :RelativePath, :Name, :ParentRelativePath, :ItemType, :Size, :ModifiedTime, :ContentHash, :ObservedTime, :ScanId)";
        public const string UpdateRemoteObservation = @"
update REMOTE_OBSERVED_SNAPSHOT set
    RemoteItemId = :RemoteItemId,
    ExistsFlag = :ExistsFlag,
    Removed = :Removed,
    Name = :Name,
    RemoteParentId = :RemoteParentId,
    ItemType = :ItemType,
    MimeType = :MimeType,
    Size = :Size,
    ContentHash = :ContentHash,
    CreatedTime = :CreatedTime,
    ModifiedTime = :ModifiedTime,
    ProviderVersion = :ProviderVersion,
    Trashed = :Trashed,
    ProviderChangeTime = :ProviderChangeTime,
    ObservedTime = :ObservedTime
where TrackedItemId = :TrackedItemId";
        public const string InsertRemoteObservation = @"
insert into REMOTE_OBSERVED_SNAPSHOT
    (TrackedItemId, RemoteItemId, ExistsFlag, Removed, Name, RemoteParentId, ItemType, MimeType, Size, ContentHash, CreatedTime, ModifiedTime, ProviderVersion, Trashed, ProviderChangeTime, ObservedTime)
values
    (:TrackedItemId, :RemoteItemId, :ExistsFlag, :Removed, :Name, :RemoteParentId, :ItemType, :MimeType, :Size, :ContentHash, :CreatedTime, :ModifiedTime, :ProviderVersion, :Trashed, :ProviderChangeTime, :ObservedTime)";
        public const string UpdateRemoteCheckpoint = @"
update REMOTE_CHECKPOINT set
    ProviderName = :ProviderName,
    ConnectionId = :ConnectionId,
    StartPageToken = :StartPageToken,
    UpdatedTime = :UpdatedTime
where SyncRootId = :SyncRootId";
        public const string InsertRemoteCheckpoint = @"
insert into REMOTE_CHECKPOINT
    (SyncRootId, ProviderName, ConnectionId, StartPageToken, UpdatedTime)
values
    (:SyncRootId, :ProviderName, :ConnectionId, :StartPageToken, :UpdatedTime)";
        public const string UpdateSyncConflict = @"
update SYNC_CONFLICT set
    SyncRootId = :SyncRootId,
    DiffKind = :DiffKind,
    DecisionKind = :DecisionKind,
    State = :State,
    Message = :Message,
    LastObservedTime = :LastObservedTime,
    ResolvedTime = :ResolvedTime
where TrackedItemId = :TrackedItemId and State = :State";
        public const string InsertSyncConflict = @"
insert into SYNC_CONFLICT
    (Id, SyncRootId, TrackedItemId, DiffKind, DecisionKind, State, Message, FirstObservedTime, LastObservedTime, ResolvedTime)
values
    (:Id, :SyncRootId, :TrackedItemId, :DiffKind, :DecisionKind, :State, :Message, :FirstObservedTime, :LastObservedTime, :ResolvedTime)";
        public const string ResolveSyncConflict = @"
update SYNC_CONFLICT set
    State = :State,
    ResolvedTime = :ResolvedTime
where TrackedItemId = :TrackedItemId and State = :OpenState";
    }
}

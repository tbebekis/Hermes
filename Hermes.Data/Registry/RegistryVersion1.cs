// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// First Hermes registry version.
/// </summary>
public class RegistryVersion1 : RegistryVersion
{
    // ● private

    static void RegisterModuleLog()
    {
        string SqlText = @"
select
   SYS_LOG.Id,
   SYS_LOG.Year,
   SYS_LOG.Month,
   SYS_LOG.DayOfMonth,
   SYS_LOG.LogTime,
   SYS_LOG.User,
   SYS_LOG.Host,
   SYS_LOG.Level,
   SYS_LOG.Source,
   SYS_LOG.Scope,
   SYS_LOG.EventId
from
  SYS_LOG
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("Log", ClassName: "LogDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "SYS_LOG";
        Table.KeyField = "Id";
        Table.AddId("Id").SetNullable(false);
        Table.AddInteger("Year", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddInteger("Month", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddInteger("DayOfMonth", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("LogTime", MaxLength: 20, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("User", MaxLength: 96, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("Host", MaxLength: 96, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("Level", MaxLength: 96, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("Source", MaxLength: 512, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("Scope", MaxLength: 512, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("EventId", MaxLength: 96, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddTextBlob("Message", Flags: FieldFlags.Required).SetNullable(false).SetLargeMemo();

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("DayOfMonth", FieldName: "DayOfMonth", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("Host", FieldName: "Host", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Level", FieldName: "Level", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("LogTime", FieldName: "LogTime", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Month", FieldName: "Month", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("Scope", FieldName: "Scope", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Source", FieldName: "Source", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("User", FieldName: "User", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Year", FieldName: "Year", FilterDataType: DataFieldType.Integer);
        SelectDef.ColumnTypes["Id"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Year"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["Month"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["DayOfMonth"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["LogTime"] = DataColumnType.Text;
        SelectDef.ColumnTypes["User"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Host"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Level"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Source"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Scope"] = DataColumnType.Text;
        SelectDef.ColumnTypes["EventId"] = DataColumnType.Text;
    }

    static void RegisterModuleSyncState()
    {
        string SqlText = @"
select
   SYNC_STATE.Id,
   SYNC_STATE.DriveStartPageToken,
   SYNC_STATE.LastSuccessfulSyncUtc
from
  SYNC_STATE
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("SyncState", ClassName: "SyncStateDataModule", ListSelectSql: SqlText, IsSingleSelect: true);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "SYNC_STATE";
        Table.KeyField = "Id";
        Table.AddId("Id").SetNullable(false);
        Table.AddString("DriveStartPageToken", MaxLength: 512, Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("LastSuccessfulSyncUtc", Flags: FieldFlags.None).SetNullable(true);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("DriveStartPageToken", FieldName: "DriveStartPageToken", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("LastSuccessfulSyncUtc", FieldName: "LastSuccessfulSyncUtc", FilterDataType: DataFieldType.DateTime);
        SelectDef.ColumnTypes["Id"] = DataColumnType.Text;
        SelectDef.ColumnTypes["DriveStartPageToken"] = DataColumnType.Text;
        SelectDef.ColumnTypes["LastSuccessfulSyncUtc"] = DataColumnType.DateTime;
    }
    static void RegisterModuleSyncRoot()
    {
        string SqlText = @"
select
   SYNC_ROOT.Id,
   SYNC_ROOT.ProviderName,
   SYNC_ROOT.ConnectionId,
   SYNC_ROOT.LocalRootPath,
   SYNC_ROOT.RemoteRootItemId,
   SYNC_ROOT.IsEnabled,
   SYNC_ROOT.CreatedTime
from
  SYNC_ROOT
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("SyncRoot", ClassName: "SyncRootDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "SYNC_ROOT";
        Table.KeyField = "Id";
        Table.AddId("Id").SetNullable(false);
        Table.AddString("ProviderName", MaxLength: 96, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("ConnectionId", MaxLength: 96, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("LocalRootPath", MaxLength: 1024, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("RemoteRootItemId", MaxLength: 256, Flags: FieldFlags.None).SetNullable(true);
        Table.AddBoolean("IsEnabled", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddDateTime("CreatedTime", Flags: FieldFlags.Required).SetNullable(false);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("ProviderName", FieldName: "ProviderName", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ConnectionId", FieldName: "ConnectionId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("LocalRootPath", FieldName: "LocalRootPath", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("RemoteRootItemId", FieldName: "RemoteRootItemId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("IsEnabled", FieldName: "IsEnabled", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("CreatedTime", FieldName: "CreatedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.ColumnTypes["Id"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ProviderName"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ConnectionId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["LocalRootPath"] = DataColumnType.Text;
        SelectDef.ColumnTypes["RemoteRootItemId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["IsEnabled"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["CreatedTime"] = DataColumnType.DateTime;
    }
    static void RegisterModuleRemoteCheckpoint()
    {
        string SqlText = @"
select
   REMOTE_CHECKPOINT.SyncRootId,
   REMOTE_CHECKPOINT.ProviderName,
   REMOTE_CHECKPOINT.ConnectionId,
   REMOTE_CHECKPOINT.StartPageToken,
   REMOTE_CHECKPOINT.UpdatedTime
from
  REMOTE_CHECKPOINT
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("RemoteCheckpoint", ClassName: "RemoteCheckpointDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "REMOTE_CHECKPOINT";
        Table.KeyField = "SyncRootId";
        Table.AddId("SyncRootId").SetNullable(false);
        Table.AddString("ProviderName", MaxLength: 96, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("ConnectionId", MaxLength: 96, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("StartPageToken", MaxLength: 512, Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("UpdatedTime", Flags: FieldFlags.None).SetNullable(true);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("ProviderName", FieldName: "ProviderName", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ConnectionId", FieldName: "ConnectionId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("StartPageToken", FieldName: "StartPageToken", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("UpdatedTime", FieldName: "UpdatedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.ColumnTypes["SyncRootId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ProviderName"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ConnectionId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["StartPageToken"] = DataColumnType.Text;
        SelectDef.ColumnTypes["UpdatedTime"] = DataColumnType.DateTime;
    }
    static void RegisterModuleTrackedItem()
    {
        string SqlText = @"
select
   TRACKED_ITEM.Id,
   TRACKED_ITEM.SyncRootId,
   TRACKED_ITEM.RemoteItemId,
   TRACKED_ITEM.LocalKey,
   TRACKED_ITEM.ItemType
from
  TRACKED_ITEM
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("TrackedItem", ClassName: "TrackedItemDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "TRACKED_ITEM";
        Table.KeyField = "Id";
        Table.AddId("Id").SetNullable(false);
        Table.AddString("SyncRootId", MaxLength: 40, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("RemoteItemId", MaxLength: 256, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("LocalKey", MaxLength: 1024, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ItemType", MaxLength: 32, Flags: FieldFlags.Required).SetNullable(false);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("SyncRootId", FieldName: "SyncRootId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("RemoteItemId", FieldName: "RemoteItemId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("LocalKey", FieldName: "LocalKey", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ItemType", FieldName: "ItemType", FilterDataType: DataFieldType.String);
        SelectDef.ColumnTypes["Id"] = DataColumnType.Text;
        SelectDef.ColumnTypes["SyncRootId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["RemoteItemId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["LocalKey"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ItemType"] = DataColumnType.Text;
    }
    static void RegisterModuleBaseSnapshot()
    {
        string SqlText = @"
select
   BASE_SNAPSHOT.TrackedItemId,
   BASE_SNAPSHOT.ExistsFlag,
   BASE_SNAPSHOT.ItemType,
   BASE_SNAPSHOT.Name,
   BASE_SNAPSHOT.LocalRelativePath,
   BASE_SNAPSHOT.RemoteParentId,
   BASE_SNAPSHOT.Size,
   BASE_SNAPSHOT.ContentHash,
   BASE_SNAPSHOT.CreatedTime,
   BASE_SNAPSHOT.ModifiedTime,
   BASE_SNAPSHOT.ProviderVersion,
   BASE_SNAPSHOT.Trashed,
   BASE_SNAPSHOT.CommittedTime
from
  BASE_SNAPSHOT
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("BaseSnapshot", ClassName: "BaseSnapshotDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "BASE_SNAPSHOT";
        Table.KeyField = "TrackedItemId";
        Table.AddId("TrackedItemId").SetNullable(false);
        Table.AddBoolean("ExistsFlag", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("ItemType", MaxLength: 32, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("Name", MaxLength: 512, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("LocalRelativePath", MaxLength: 1024, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("RemoteParentId", MaxLength: 256, Flags: FieldFlags.None).SetNullable(true);
        Table.AddInteger("Size", Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ContentHash", MaxLength: 128, Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("CreatedTime", Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("ModifiedTime", Flags: FieldFlags.None).SetNullable(true);
        Table.AddInteger("ProviderVersion", Flags: FieldFlags.None).SetNullable(true);
        Table.AddBoolean("Trashed", Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("CommittedTime", Flags: FieldFlags.Required).SetNullable(false);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("ExistsFlag", FieldName: "ExistsFlag", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("ItemType", FieldName: "ItemType", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Name", FieldName: "Name", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("LocalRelativePath", FieldName: "LocalRelativePath", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("RemoteParentId", FieldName: "RemoteParentId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Size", FieldName: "Size", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("ContentHash", FieldName: "ContentHash", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("CreatedTime", FieldName: "CreatedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ModifiedTime", FieldName: "ModifiedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ProviderVersion", FieldName: "ProviderVersion", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("Trashed", FieldName: "Trashed", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("CommittedTime", FieldName: "CommittedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.ColumnTypes["TrackedItemId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ExistsFlag"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["ItemType"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Name"] = DataColumnType.Text;
        SelectDef.ColumnTypes["LocalRelativePath"] = DataColumnType.Text;
        SelectDef.ColumnTypes["RemoteParentId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Size"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["ContentHash"] = DataColumnType.Text;
        SelectDef.ColumnTypes["CreatedTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ModifiedTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ProviderVersion"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["Trashed"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["CommittedTime"] = DataColumnType.DateTime;
    }
    static void RegisterModuleLocalObservedSnapshot()
    {
        string SqlText = @"
select
   LOCAL_OBSERVED_SNAPSHOT.TrackedItemId,
   LOCAL_OBSERVED_SNAPSHOT.ExistsFlag,
   LOCAL_OBSERVED_SNAPSHOT.RelativePath,
   LOCAL_OBSERVED_SNAPSHOT.Name,
   LOCAL_OBSERVED_SNAPSHOT.ParentRelativePath,
   LOCAL_OBSERVED_SNAPSHOT.ItemType,
   LOCAL_OBSERVED_SNAPSHOT.Size,
   LOCAL_OBSERVED_SNAPSHOT.ModifiedTime,
   LOCAL_OBSERVED_SNAPSHOT.ContentHash,
   LOCAL_OBSERVED_SNAPSHOT.ObservedTime,
   LOCAL_OBSERVED_SNAPSHOT.ScanId
from
  LOCAL_OBSERVED_SNAPSHOT
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("LocalObservedSnapshot", ClassName: "LocalObservedSnapshotDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "LOCAL_OBSERVED_SNAPSHOT";
        Table.KeyField = "TrackedItemId";
        Table.AddId("TrackedItemId").SetNullable(false);
        Table.AddBoolean("ExistsFlag", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("RelativePath", MaxLength: 1024, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("Name", MaxLength: 512, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ParentRelativePath", MaxLength: 1024, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ItemType", MaxLength: 32, Flags: FieldFlags.None).SetNullable(true);
        Table.AddInteger("Size", Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("ModifiedTime", Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ContentHash", MaxLength: 128, Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("ObservedTime", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("ScanId", MaxLength: 40, Flags: FieldFlags.None).SetNullable(true);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("ExistsFlag", FieldName: "ExistsFlag", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("RelativePath", FieldName: "RelativePath", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Name", FieldName: "Name", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ParentRelativePath", FieldName: "ParentRelativePath", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ItemType", FieldName: "ItemType", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Size", FieldName: "Size", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("ModifiedTime", FieldName: "ModifiedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ContentHash", FieldName: "ContentHash", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ObservedTime", FieldName: "ObservedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ScanId", FieldName: "ScanId", FilterDataType: DataFieldType.String);
        SelectDef.ColumnTypes["TrackedItemId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ExistsFlag"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["RelativePath"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Name"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ParentRelativePath"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ItemType"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Size"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["ModifiedTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ContentHash"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ObservedTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ScanId"] = DataColumnType.Text;
    }
    static void RegisterModuleRemoteObservedSnapshot()
    {
        string SqlText = @"
select
   REMOTE_OBSERVED_SNAPSHOT.TrackedItemId,
   REMOTE_OBSERVED_SNAPSHOT.RemoteItemId,
   REMOTE_OBSERVED_SNAPSHOT.ExistsFlag,
   REMOTE_OBSERVED_SNAPSHOT.Removed,
   REMOTE_OBSERVED_SNAPSHOT.Name,
   REMOTE_OBSERVED_SNAPSHOT.RemoteParentId,
   REMOTE_OBSERVED_SNAPSHOT.ItemType,
   REMOTE_OBSERVED_SNAPSHOT.MimeType,
   REMOTE_OBSERVED_SNAPSHOT.Size,
   REMOTE_OBSERVED_SNAPSHOT.ContentHash,
   REMOTE_OBSERVED_SNAPSHOT.CreatedTime,
   REMOTE_OBSERVED_SNAPSHOT.ModifiedTime,
   REMOTE_OBSERVED_SNAPSHOT.ProviderVersion,
   REMOTE_OBSERVED_SNAPSHOT.Trashed,
   REMOTE_OBSERVED_SNAPSHOT.ProviderChangeTime,
   REMOTE_OBSERVED_SNAPSHOT.ObservedTime
from
  REMOTE_OBSERVED_SNAPSHOT
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("RemoteObservedSnapshot", ClassName: "RemoteObservedSnapshotDataModule", ListSelectSql: SqlText);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "REMOTE_OBSERVED_SNAPSHOT";
        Table.KeyField = "TrackedItemId";
        Table.AddId("TrackedItemId").SetNullable(false);
        Table.AddString("RemoteItemId", MaxLength: 256, Flags: FieldFlags.Required).SetNullable(false);
        Table.AddBoolean("ExistsFlag", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddBoolean("Removed", Flags: FieldFlags.Required).SetNullable(false);
        Table.AddString("Name", MaxLength: 512, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("RemoteParentId", MaxLength: 256, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ItemType", MaxLength: 32, Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("MimeType", MaxLength: 256, Flags: FieldFlags.None).SetNullable(true);
        Table.AddInteger("Size", Flags: FieldFlags.None).SetNullable(true);
        Table.AddString("ContentHash", MaxLength: 128, Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("CreatedTime", Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("ModifiedTime", Flags: FieldFlags.None).SetNullable(true);
        Table.AddInteger("ProviderVersion", Flags: FieldFlags.None).SetNullable(true);
        Table.AddBoolean("Trashed", Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("ProviderChangeTime", Flags: FieldFlags.None).SetNullable(true);
        Table.AddDateTime("ObservedTime", Flags: FieldFlags.Required).SetNullable(false);

        SelectDef SelectDef = Module.SelectList[0];
        SelectDef.AddFilter("RemoteItemId", FieldName: "RemoteItemId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ExistsFlag", FieldName: "ExistsFlag", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("Removed", FieldName: "Removed", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("Name", FieldName: "Name", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("RemoteParentId", FieldName: "RemoteParentId", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("ItemType", FieldName: "ItemType", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("MimeType", FieldName: "MimeType", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("Size", FieldName: "Size", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("ContentHash", FieldName: "ContentHash", FilterDataType: DataFieldType.String);
        SelectDef.AddFilter("CreatedTime", FieldName: "CreatedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ModifiedTime", FieldName: "ModifiedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ProviderVersion", FieldName: "ProviderVersion", FilterDataType: DataFieldType.Integer);
        SelectDef.AddFilter("Trashed", FieldName: "Trashed", FilterDataType: DataFieldType.Boolean);
        SelectDef.AddFilter("ProviderChangeTime", FieldName: "ProviderChangeTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.AddFilter("ObservedTime", FieldName: "ObservedTime", FilterDataType: DataFieldType.DateTime);
        SelectDef.ColumnTypes["TrackedItemId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["RemoteItemId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ExistsFlag"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["Removed"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["Name"] = DataColumnType.Text;
        SelectDef.ColumnTypes["RemoteParentId"] = DataColumnType.Text;
        SelectDef.ColumnTypes["ItemType"] = DataColumnType.Text;
        SelectDef.ColumnTypes["MimeType"] = DataColumnType.Text;
        SelectDef.ColumnTypes["Size"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["ContentHash"] = DataColumnType.Text;
        SelectDef.ColumnTypes["CreatedTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ModifiedTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ProviderVersion"] = DataColumnType.Integer;
        SelectDef.ColumnTypes["Trashed"] = DataColumnType.Boolean;
        SelectDef.ColumnTypes["ProviderChangeTime"] = DataColumnType.DateTime;
        SelectDef.ColumnTypes["ObservedTime"] = DataColumnType.DateTime;
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryVersion1"/> class.
    /// </summary>
    public RegistryVersion1()
    {
    }

    // ● public

    /// <inheritdoc/>
    public override void RegisterModules()
    {
        RegisterModuleLog();
        RegisterModuleSyncState();
        RegisterModuleSyncRoot();
        RegisterModuleRemoteCheckpoint();
        RegisterModuleTrackedItem();
        RegisterModuleBaseSnapshot();
        RegisterModuleLocalObservedSnapshot();
        RegisterModuleRemoteObservedSnapshot();
    }

    // ● properties

    /// <inheritdoc/>
    public override int VersionNumber { get; } = 1;
}

// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// First Hermes registry version.
/// </summary>
public class RegistryVersion1 : RegistryVersion
{
    // ● private

    static private void RegisterModuleLog()
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

    static private void RegisterModuleSyncState()
    {
        string SqlText = @"
select
   HERMES_SYNC_STATE.Id,
   HERMES_SYNC_STATE.DriveStartPageToken,
   HERMES_SYNC_STATE.LastSuccessfulSyncUtc
from
  HERMES_SYNC_STATE
";
        ModuleDef Module = DataRegistry.AddOrUpdateModule("SyncState", ClassName: "SyncStateDataModule", ListSelectSql: SqlText, IsSingleSelect: true);
        if (Module.Table.Fields.Count > 0)
            return;

        TableDef Table = Module.Table;
        Table.Name = "HERMES_SYNC_STATE";
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
    }

    // ● properties

    /// <inheritdoc/>
    public override int VersionNumber { get; } = 1;
}

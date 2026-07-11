// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// First Hermes database schema version.
/// </summary>
public class SchemaVersion1 : SchemaVersionDef
{
    // ● protected

    /// <inheritdoc/>
    protected override void RegisterInternal()
    {
        string SqlText = @"
CREATE TABLE SYS_LOG (
    Id  @NVARCHAR(40)  @NOT_NULL primary key
    ,Year int @NOT_NULL
    ,Month int @NOT_NULL
    ,DayOfMonth int @NOT_NULL
    ,LogTime @NVARCHAR(20) @NOT_NULL
    ,User @NVARCHAR(96) @NOT_NULL
    ,Host @NVARCHAR(96) @NOT_NULL
    ,Level @NVARCHAR(96) @NOT_NULL
    ,Source @NVARCHAR(512) @NOT_NULL
    ,Scope @NVARCHAR(512) @NOT_NULL
    ,EventId @NVARCHAR(96) @NOT_NULL
    ,Message @NBLOB_TEXT @NOT_NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE SYNC_STATE (
    Id @NVARCHAR(40) @NOT_NULL primary key
    ,DriveStartPageToken @NVARCHAR(512) @NULL
    ,LastSuccessfulSyncUtc @DATE_TIME @NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE SYNC_ROOT (
    Id @NVARCHAR(40) @NOT_NULL primary key
    ,ProviderName @NVARCHAR(96) @NOT_NULL
    ,ConnectionId @NVARCHAR(96) @NULL
    ,LocalRootPath @NVARCHAR(1024) @NOT_NULL
    ,RemoteRootItemId @NVARCHAR(256) @NULL
    ,IsEnabled @BOOL @NOT_NULL
    ,CreatedTime @DATE_TIME @NOT_NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE REMOTE_CHECKPOINT (
    SyncRootId @NVARCHAR(40) @NOT_NULL primary key
    ,ProviderName @NVARCHAR(96) @NOT_NULL
    ,ConnectionId @NVARCHAR(96) @NULL
    ,StartPageToken @NVARCHAR(512) @NULL
    ,UpdatedTime @DATE_TIME @NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE TRACKED_ITEM (
    Id @NVARCHAR(40) @NOT_NULL primary key
    ,SyncRootId @NVARCHAR(40) @NOT_NULL
    ,RemoteItemId @NVARCHAR(256) @NULL
    ,LocalKey @NVARCHAR(1024) @NULL
    ,ItemType @NVARCHAR(32) @NOT_NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE BASE_SNAPSHOT (
    TrackedItemId @NVARCHAR(40) @NOT_NULL primary key
    ,ExistsFlag @BOOL @NOT_NULL
    ,ItemType @NVARCHAR(32) @NULL
    ,Name @NVARCHAR(512) @NULL
    ,LocalRelativePath @NVARCHAR(1024) @NULL
    ,RemoteParentId @NVARCHAR(256) @NULL
    ,Size int @NULL
    ,ContentHash @NVARCHAR(128) @NULL
    ,CreatedTime @DATE_TIME @NULL
    ,ModifiedTime @DATE_TIME @NULL
    ,ProviderVersion int @NULL
    ,Trashed @BOOL @NULL
    ,CommittedTime @DATE_TIME @NOT_NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE LOCAL_OBSERVED_SNAPSHOT (
    TrackedItemId @NVARCHAR(40) @NOT_NULL primary key
    ,ExistsFlag @BOOL @NOT_NULL
    ,RelativePath @NVARCHAR(1024) @NULL
    ,Name @NVARCHAR(512) @NULL
    ,ParentRelativePath @NVARCHAR(1024) @NULL
    ,ItemType @NVARCHAR(32) @NULL
    ,Size int @NULL
    ,ModifiedTime @DATE_TIME @NULL
    ,ContentHash @NVARCHAR(128) @NULL
    ,ObservedTime @DATE_TIME @NOT_NULL
    ,ScanId @NVARCHAR(40) @NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE REMOTE_OBSERVED_SNAPSHOT (
    TrackedItemId @NVARCHAR(40) @NOT_NULL primary key
    ,RemoteItemId @NVARCHAR(256) @NOT_NULL
    ,ExistsFlag @BOOL @NOT_NULL
    ,Removed @BOOL @NOT_NULL
    ,Name @NVARCHAR(512) @NULL
    ,RemoteParentId @NVARCHAR(256) @NULL
    ,ItemType @NVARCHAR(32) @NULL
    ,MimeType @NVARCHAR(256) @NULL
    ,Size int @NULL
    ,ContentHash @NVARCHAR(128) @NULL
    ,CreatedTime @DATE_TIME @NULL
    ,ModifiedTime @DATE_TIME @NULL
    ,ProviderVersion int @NULL
    ,Trashed @BOOL @NULL
    ,ProviderChangeTime @DATE_TIME @NULL
    ,ObservedTime @DATE_TIME @NOT_NULL
    )
";
        Version.AddTable(SqlText);

        SqlText = @"
CREATE TABLE SYNC_CONFLICT (
    Id @NVARCHAR(40) @NOT_NULL primary key
    ,SyncRootId @NVARCHAR(40) @NOT_NULL
    ,TrackedItemId @NVARCHAR(40) @NOT_NULL
    ,DiffKind @NVARCHAR(64) @NOT_NULL
    ,DecisionKind @NVARCHAR(64) @NOT_NULL
    ,State @NVARCHAR(32) @NOT_NULL
    ,Message @NBLOB_TEXT @NULL
    ,FirstObservedTime @DATE_TIME @NOT_NULL
    ,LastObservedTime @DATE_TIME @NOT_NULL
    ,ResolvedTime @DATE_TIME @NULL
    )
";
        Version.AddTable(SqlText);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersion1"/> class.
    /// </summary>
    public SchemaVersion1()
    {
    }

    // ● properties

    /// <inheritdoc/>
    public override int VersionNumber { get; } = 1;
}

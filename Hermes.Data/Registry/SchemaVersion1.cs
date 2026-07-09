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
CREATE TABLE HERMES_SYNC_STATE (
    Id @NVARCHAR(40) @NOT_NULL primary key
    ,DriveStartPageToken @NVARCHAR(512) @NULL
    ,LastSuccessfulSyncUtc @DATE_TIME @NULL
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

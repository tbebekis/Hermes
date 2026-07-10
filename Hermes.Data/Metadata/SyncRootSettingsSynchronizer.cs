// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Synchronizes the configured sync root settings with the metadata store.
/// </summary>
static public class SyncRootSettingsSynchronizer
{
    // ● public

    /// <summary>
    /// Inserts or updates the configured sync root.
    /// </summary>
    static public SyncRootRecord EnsureSyncRoot(SqlMetadataStore Store, SyncSettings Settings, DateTime CreatedTime)
    {
        Guard.NotNull(Store, nameof(Store));
        Guard.NotNull(Settings, nameof(Settings));
        Guard.NotNullOrWhiteSpace(Settings.SyncRootId, nameof(Settings.SyncRootId));
        Guard.NotNullOrWhiteSpace(Settings.LocalRootPath, nameof(Settings.LocalRootPath));
        Guard.NotNullOrWhiteSpace(Settings.RemoteRootFolderId, nameof(Settings.RemoteRootFolderId));

        SyncRootRecord Record = Store.GetSyncRoot(Settings.SyncRootId);

        if (Record == null)
        {
            Record = new SyncRootRecord()
            {
                Id = Settings.SyncRootId,
                ProviderName = "GoogleDrive",
                ConnectionId = "default",
                LocalRootPath = Settings.LocalRootPath,
                RemoteRootItemId = Settings.RemoteRootFolderId,
                IsEnabled = true,
                CreatedTime = CreatedTime,
            };
            Store.InsertSyncRoot(Record);
            return Record;
        }

        Record.ProviderName = "GoogleDrive";
        Record.ConnectionId = string.IsNullOrWhiteSpace(Record.ConnectionId) ? "default" : Record.ConnectionId;
        Record.LocalRootPath = Settings.LocalRootPath;
        Record.RemoteRootItemId = Settings.RemoteRootFolderId;
        Record.IsEnabled = true;
        Store.UpdateSyncRoot(Record);

        return Record;
    }
}

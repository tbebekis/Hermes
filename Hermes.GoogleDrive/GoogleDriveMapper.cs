// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Maps Google Drive API models to storage models.
/// </summary>
public class GoogleDriveMapper
{
    // ● private

    string GetParentId(DriveFile File)
    {
        if (File.Parents == null || File.Parents.Count == 0)
            return string.Empty;

        return File.Parents[0] ?? string.Empty;
    }

    long GetSize(DriveFile File)
    {
        if (File.Size.HasValue)
            return File.Size.Value;

        return 0;
    }

    // ● public

    /// <summary>
    /// Maps Drive account information.
    /// </summary>
    public GoogleDriveAbout MapAbout(About About, string RootFolderId)
    {
        Guard.NotNull(About, nameof(About));

        string DisplayName = About.User?.DisplayName ?? string.Empty;
        string EmailAddress = About.User?.EmailAddress ?? string.Empty;
        long StorageLimit = About.StorageQuota?.Limit ?? 0;
        long StorageUsage = About.StorageQuota?.Usage ?? 0;

        return new GoogleDriveAbout(DisplayName, EmailAddress, RootFolderId, StorageLimit, StorageUsage);
    }

    /// <summary>
    /// Maps a Google Drive file to a storage item.
    /// </summary>
    public StorageItem MapFile(DriveFile File)
    {
        Guard.NotNull(File, nameof(File));

        string Id = File.Id ?? string.Empty;
        string Name = File.Name ?? Id;
        string ParentId = GetParentId(File);
        string Path = "/" + Name;
        string MimeType = File.MimeType ?? string.Empty;
        DateTimeOffset CreatedTime = File.CreatedTimeDateTimeOffset ?? default;
        DateTimeOffset ModifiedTime = File.ModifiedTimeDateTimeOffset ?? default;
        long Version = File.Version ?? 0;
        bool Trashed = File.Trashed == true;

        if (MimeType == GoogleDriveConstants.FolderMimeType)
            return new StorageFolder(Id, ParentId, Name, Path, MimeType, CreatedTime, ModifiedTime, Version, Trashed);

        return new StorageFile(Id, ParentId, Name, Path, MimeType, GetSize(File), File.Md5Checksum ?? string.Empty, CreatedTime, ModifiedTime, Version, Trashed);
    }

    /// <summary>
    /// Maps a Google Drive change to a storage change.
    /// </summary>
    public StorageChange MapChange(Change Change)
    {
        Guard.NotNull(Change, nameof(Change));

        StorageChangeType ChangeType = Change.Removed == true ? StorageChangeType.Deleted : StorageChangeType.Updated;
        StorageItem Item;

        if (Change.File != null)
        {
            Item = MapFile(Change.File);
        }
        else
        {
            string FileId = Change.FileId ?? "unknown";
            Item = new StorageItem(FileId, string.Empty, FileId, "/" + FileId, StorageItemKind.File, string.Empty, 0, string.Empty, default, default, 0, true);
        }

        return new StorageChange(Change.FileId ?? Item.Id, ChangeType, Item);
    }
}

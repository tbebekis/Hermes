// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Maps Google Drive API models to storage models.
/// </summary>
public class GoogleDriveMapper
{
    // ● public

    /// <summary>
    /// Creates a placeholder storage folder.
    /// </summary>
    public StorageFolder CreateRootFolder(string FolderId)
    {
        return new StorageFolder(FolderId, string.Empty, "My Drive", "/");
    }
}

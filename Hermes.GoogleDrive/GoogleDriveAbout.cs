// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Contains basic information about the authenticated Google Drive account.
/// </summary>
public class GoogleDriveAbout
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveAbout"/> class.
    /// </summary>
    public GoogleDriveAbout(string DisplayName, string EmailAddress, string RootFolderId, long StorageLimit, long StorageUsage)
    {
        this.DisplayName = DisplayName ?? string.Empty;
        this.EmailAddress = EmailAddress ?? string.Empty;
        this.RootFolderId = RootFolderId ?? string.Empty;
        this.StorageLimit = StorageLimit;
        this.StorageUsage = StorageUsage;
    }

    // ● properties

    /// <summary>
    /// Gets the account display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the account email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the Drive root folder id.
    /// </summary>
    public string RootFolderId { get; }

    /// <summary>
    /// Gets the storage limit in bytes.
    /// </summary>
    public long StorageLimit { get; }

    /// <summary>
    /// Gets the storage usage in bytes.
    /// </summary>
    public long StorageUsage { get; }
}

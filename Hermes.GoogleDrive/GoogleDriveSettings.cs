// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Contains Google Drive provider settings.
/// </summary>
public class GoogleDriveSettings
{
    // ● properties

    /// <summary>
    /// Gets or sets the OAuth client id.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote root folder id.
    /// </summary>
    public string RootFolderId { get; set; } = string.Empty;
}

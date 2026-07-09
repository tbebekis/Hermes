// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Represents a complete Google Drive changes list response across all pages.
/// </summary>
public class GoogleDriveChangeListResult
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveChangeListResult"/> class.
    /// </summary>
    public GoogleDriveChangeListResult(string StartPageToken, string NewStartPageToken, IReadOnlyList<GoogleDriveChangeItem> Changes)
    {
        this.StartPageToken = StartPageToken ?? string.Empty;
        this.NewStartPageToken = NewStartPageToken ?? string.Empty;
        this.Changes = Guard.NotNull(Changes, nameof(Changes));
    }

    // ● properties

    /// <summary>
    /// Gets the start page token used for this request.
    /// </summary>
    public string StartPageToken { get; }
    /// <summary>
    /// Gets the new start page token returned by Google Drive.
    /// </summary>
    public string NewStartPageToken { get; }
    /// <summary>
    /// Gets all returned changes.
    /// </summary>
    public IReadOnlyList<GoogleDriveChangeItem> Changes { get; }
}

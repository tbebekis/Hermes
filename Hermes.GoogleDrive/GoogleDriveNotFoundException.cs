// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Exception thrown when a Google Drive item is not found.
/// </summary>
public class GoogleDriveNotFoundException : HermesException
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveNotFoundException"/> class.
    /// </summary>
    public GoogleDriveNotFoundException(string FileId, Exception InnerException)
        : base($"Google Drive item was not found. FileId: {FileId}", InnerException)
    {
        this.FileId = FileId ?? string.Empty;
    }

    // ● properties

    /// <summary>
    /// Gets the file id that was not found.
    /// </summary>
    public string FileId { get; }
}

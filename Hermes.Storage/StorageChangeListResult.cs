// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents a complete remote storage change list response.
/// </summary>
public class StorageChangeListResult
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageChangeListResult"/> class.
    /// </summary>
    public StorageChangeListResult(string StartPageToken, string NewStartPageToken, IReadOnlyList<StorageChange> Changes)
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
    /// Gets the new start page token returned by the provider.
    /// </summary>
    public string NewStartPageToken { get; }
    /// <summary>
    /// Gets all returned changes.
    /// </summary>
    public IReadOnlyList<StorageChange> Changes { get; }
}

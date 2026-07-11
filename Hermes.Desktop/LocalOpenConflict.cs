// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Open conflict item returned by the local service HTTP API.
/// </summary>
public class LocalOpenConflict
{
    // ● properties

    /// <summary>
    /// Gets or sets the conflict id.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the diff kind.
    /// </summary>
    public string DiffKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the decision kind.
    /// </summary>
    public string DecisionKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the conflict message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the local relative path.
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote item name.
    /// </summary>
    public string RemoteName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last observed time.
    /// </summary>
    public DateTime LastObservedTime { get; set; }
}

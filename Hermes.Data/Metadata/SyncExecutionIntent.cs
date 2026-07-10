// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains an executor-facing interpretation of a synchronization execution request.
/// </summary>
public class SyncExecutionIntent
{
    // ● properties

    /// <summary>
    /// Gets or sets the source execution request.
    /// </summary>
    public SyncExecutionRequest Request { get; set; }

    /// <summary>
    /// Gets or sets the executor-facing intent kind.
    /// </summary>
    public SyncExecutionIntentKind IntentKind { get; set; }

    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved remote item id.
    /// </summary>
    public string RemoteItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved local relative path.
    /// </summary>
    public string LocalRelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source local relative path for local namespace operations.
    /// </summary>
    public string SourceLocalRelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved item type.
    /// </summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved remote parent id.
    /// </summary>
    public string RemoteParentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source remote parent id for remote namespace operations.
    /// </summary>
    public string SourceRemoteParentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved content hash.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved content size.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this intent is ready for normal executor work.
    /// </summary>
    public bool CanExecute { get; set; }

    /// <summary>
    /// Gets validation messages explaining why this intent cannot be executed normally.
    /// </summary>
    public List<string> ValidationMessages { get; } = new();
}

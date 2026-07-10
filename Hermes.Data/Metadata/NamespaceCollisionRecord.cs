// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents a detected namespace collision.
/// </summary>
public class NamespaceCollisionRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the remote parent id where the collision was detected.
    /// </summary>
    public string RemoteParentId { get; set; }
    /// <summary>
    /// Gets or sets the colliding item name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets the tracked item ids in collision.
    /// </summary>
    public List<string> TrackedItemIds { get; } = new();
}

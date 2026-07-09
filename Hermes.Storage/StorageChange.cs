// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents a remote storage change.
/// </summary>
public class StorageChange
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageChange"/> class.
    /// </summary>
    public StorageChange(string ChangeId, StorageChangeType ChangeType, StorageItem Item)
    {
        this.ChangeId = Guard.NotNullOrWhiteSpace(ChangeId, nameof(ChangeId));
        this.ChangeType = ChangeType;
        this.Item = Guard.NotNull(Item, nameof(Item));
    }

    // ● properties

    /// <summary>
    /// Gets the provider-specific change id.
    /// </summary>
    public string ChangeId { get; }

    /// <summary>
    /// Gets the change type.
    /// </summary>
    public StorageChangeType ChangeType { get; }

    /// <summary>
    /// Gets the changed item.
    /// </summary>
    public StorageItem Item { get; }
}

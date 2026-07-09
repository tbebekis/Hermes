// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Represents a synchronization operation.
/// </summary>
public class SyncOperation
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncOperation"/> class.
    /// </summary>
    public SyncOperation(SyncOperationType OperationType, string LocalPath, string StorageItemId)
    {
        this.OperationId = Guid.NewGuid().ToString("N");
        this.OperationType = OperationType;
        this.LocalPath = LocalPath ?? string.Empty;
        this.StorageItemId = StorageItemId ?? string.Empty;
    }

    // ● properties

    /// <summary>
    /// Gets the operation id.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the operation type.
    /// </summary>
    public SyncOperationType OperationType { get; }

    /// <summary>
    /// Gets the local path.
    /// </summary>
    public string LocalPath { get; }

    /// <summary>
    /// Gets the storage item id.
    /// </summary>
    public string StorageItemId { get; }
}

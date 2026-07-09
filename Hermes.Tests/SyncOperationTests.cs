// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests sync operation creation.
/// </summary>
public class SyncOperationTests
{
    // ● public

    /// <summary>
    /// Verifies that sync operations expose constructor values.
    /// </summary>
    [Fact]
    public void SyncOperationStoresValues()
    {
        SyncOperation Operation = new(SyncOperationType.Upload, "/tmp/file.txt", "drive-id");

        Assert.False(string.IsNullOrWhiteSpace(Operation.OperationId));
        Assert.Equal(SyncOperationType.Upload, Operation.OperationType);
        Assert.Equal("/tmp/file.txt", Operation.LocalPath);
        Assert.Equal("drive-id", Operation.StorageItemId);
    }
}

// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests metadata record mapping to synchronization item state.
/// </summary>
public class SyncItemStateMapperTests
{
    // ● private

    static readonly DateTime Time = new(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
    static BaseSnapshotRecord BaseSnapshot() => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = true,
        ItemType = "File",
        Name = "Report.txt",
        LocalRelativePath = "Report.txt",
        RemoteParentId = "remote-parent",
        Size = 42,
        ContentHash = "hash-1",
        ProviderVersion = 1,
        Trashed = false,
        CommittedTime = Time,
    };
    static LocalObservedSnapshotRecord LocalObservation(bool Exists = true, string Name = "Report.txt", string Hash = "hash-1") => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = Exists,
        RelativePath = Exists ? Name : null,
        Name = Exists ? Name : null,
        ItemType = Exists ? "File" : null,
        Size = Exists ? 42 : null,
        ContentHash = Exists ? Hash : null,
        ObservedTime = Time,
        ScanId = "scan-1",
    };
    static RemoteObservedSnapshotRecord RemoteObservation(bool Exists = true, bool Removed = false, string Name = "Report.txt", string Hash = "hash-1") => new()
    {
        TrackedItemId = "item-1",
        RemoteItemId = "remote-1",
        ExistsFlag = Exists,
        Removed = Removed,
        Name = Exists ? Name : null,
        RemoteParentId = Exists ? "remote-parent" : null,
        ItemType = Exists ? "File" : null,
        Size = Exists ? 42 : null,
        ContentHash = Exists ? Hash : null,
        ProviderVersion = Exists ? 1 : null,
        Trashed = Exists ? false : null,
        ObservedTime = Time,
    };
    static SyncDiffKind Classify(SyncDiffInput Input)
    {
        SyncDiffClassifier Classifier = new();
        return Classifier.Classify(Input);
    }

    // ● public

    /// <summary>
    /// Verifies unchanged persisted records map to no change.
    /// </summary>
    [Fact]
    public void CreateDiffInputMapsUnchangedRecordsToNoChange()
    {
        SyncDiffInput Input = SyncItemStateMapper.CreateDiffInput(BaseSnapshot(), LocalObservation(), RemoteObservation());

        Assert.Equal(SyncDiffKind.NoChange, Classify(Input));
    }
    /// <summary>
    /// Verifies local observation changes are preserved.
    /// </summary>
    [Fact]
    public void CreateDiffInputPreservesLocalChange()
    {
        SyncDiffInput Input = SyncItemStateMapper.CreateDiffInput(BaseSnapshot(), LocalObservation(Hash: "hash-local"), RemoteObservation());

        Assert.Equal(SyncDiffKind.LocalChanged, Classify(Input));
    }
    /// <summary>
    /// Verifies remote observation changes are preserved.
    /// </summary>
    [Fact]
    public void CreateDiffInputPreservesRemoteChange()
    {
        SyncDiffInput Input = SyncItemStateMapper.CreateDiffInput(BaseSnapshot(), LocalObservation(), RemoteObservation(Hash: "hash-remote"));

        Assert.Equal(SyncDiffKind.RemoteChanged, Classify(Input));
    }
    /// <summary>
    /// Verifies local missing observations are preserved.
    /// </summary>
    [Fact]
    public void CreateDiffInputPreservesLocalMissing()
    {
        SyncDiffInput Input = SyncItemStateMapper.CreateDiffInput(BaseSnapshot(), LocalObservation(false), RemoteObservation());

        Assert.Equal(SyncDiffKind.LocalMissing, Classify(Input));
    }
    /// <summary>
    /// Verifies permanent remote removal tombstones are preserved.
    /// </summary>
    [Fact]
    public void CreateDiffInputPreservesRemoteRemoval()
    {
        SyncDiffInput Input = SyncItemStateMapper.CreateDiffInput(BaseSnapshot(), LocalObservation(), RemoteObservation(false, true));

        Assert.Equal(SyncDiffKind.RemoteRemoved, Classify(Input));
    }
}

// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests three-way synchronization diff classification.
/// </summary>
public class SyncDiffClassifierTests
{
    // ● private

    static SyncItemState State(string Name = "Report.txt", string Hash = "hash-1", bool Exists = true) => new()
    {
        Exists = Exists,
        ItemType = "File",
        Name = Name,
        LocalRelativePath = Name,
        RemoteParentId = "remote-parent",
        ContentHash = Hash,
        Size = 42,
        ProviderVersion = 1,
    };
    static SyncDiffKind Classify(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState, bool Collision = false)
    {
        SyncDiffClassifier Classifier = new();
        return Classifier.Classify(new SyncDiffInput()
        {
            BaseState = BaseState,
            LocalState = LocalState,
            RemoteState = RemoteState,
            NamespaceCollisionDetected = Collision,
        });
    }

    // ● public

    /// <summary>
    /// Verifies unchanged state classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsNoChangeWhenObservedStatesMatchBase()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(), State());

        Assert.Equal(SyncDiffKind.NoChange, Kind);
    }
    /// <summary>
    /// Verifies provider version drift alone does not require endpoint mutation.
    /// </summary>
    [Fact]
    public void ClassifyReturnsNoChangeWhenOnlyProviderVersionChanged()
    {
        SyncItemState RemoteState = State();
        RemoteState.ProviderVersion = 2;

        SyncDiffKind Kind = Classify(State(), State(), RemoteState);

        Assert.Equal(SyncDiffKind.NoChange, Kind);
    }
    /// <summary>
    /// Verifies local-only change classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsLocalChangedWhenOnlyLocalChanged()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(Hash: "hash-local"), State());

        Assert.Equal(SyncDiffKind.LocalChanged, Kind);
    }
    /// <summary>
    /// Verifies remote-only change classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteChangedWhenOnlyRemoteChanged()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(), State(Hash: "hash-remote"));

        Assert.Equal(SyncDiffKind.RemoteChanged, Kind);
    }
    /// <summary>
    /// Verifies local-only new item classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsLocalChangedWhenNewItemExistsOnlyLocally()
    {
        SyncDiffKind Kind = Classify(null, State(), null);

        Assert.Equal(SyncDiffKind.LocalChanged, Kind);
    }
    /// <summary>
    /// Verifies remote-only new item classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteChangedWhenNewItemExistsOnlyRemotely()
    {
        SyncDiffKind Kind = Classify(null, null, State());

        Assert.Equal(SyncDiffKind.RemoteChanged, Kind);
    }
    /// <summary>
    /// Verifies matching new item classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsBothChangedCompatibleWhenNewItemExistsOnBothSides()
    {
        SyncDiffKind Kind = Classify(null, State(), State());

        Assert.Equal(SyncDiffKind.BothChangedCompatible, Kind);
    }
    /// <summary>
    /// Verifies compatible two-sided change classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsBothChangedCompatibleWhenBothSidesMatch()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(Hash: "hash-2"), State(Hash: "hash-2"));

        Assert.Equal(SyncDiffKind.BothChangedCompatible, Kind);
    }
    /// <summary>
    /// Verifies conflicting two-sided change classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenBothSidesDiffer()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(Hash: "hash-local"), State(Hash: "hash-remote"));

        Assert.Equal(SyncDiffKind.Conflict, Kind);
    }
    /// <summary>
    /// Verifies local missing classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsLocalMissingWhenLocalWasNotObserved()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(Exists: false), State());

        Assert.Equal(SyncDiffKind.LocalMissing, Kind);
    }
    /// <summary>
    /// Verifies remote missing classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteMissingWhenRemoteWasNotObserved()
    {
        SyncItemState BaseState = State();

        SyncDiffKind Kind = Classify(BaseState, State(), State(Exists: false));

        Assert.Equal(SyncDiffKind.RemoteMissing, Kind);
    }
    /// <summary>
    /// Verifies remote trashed classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteTrashedWhenRemoteItemIsTrashed()
    {
        SyncItemState RemoteState = State();
        RemoteState.Trashed = true;

        SyncDiffKind Kind = Classify(State(), State(), RemoteState);

        Assert.Equal(SyncDiffKind.RemoteTrashed, Kind);
    }
    /// <summary>
    /// Verifies remote permanent removal classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteRemovedWhenRemoteChangeIsTombstone()
    {
        SyncItemState RemoteState = State(Exists: false);
        RemoteState.Removed = true;

        SyncDiffKind Kind = Classify(State(), State(), RemoteState);

        Assert.Equal(SyncDiffKind.RemoteRemoved, Kind);
    }
    /// <summary>
    /// Verifies namespace collision classification.
    /// </summary>
    [Fact]
    public void ClassifyReturnsNamespaceCollisionWhenCollisionDetected()
    {
        SyncDiffKind Kind = Classify(State(), State(), State(), true);

        Assert.Equal(SyncDiffKind.NamespaceCollision, Kind);
    }
}

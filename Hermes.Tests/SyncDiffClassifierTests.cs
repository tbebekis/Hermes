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
    /// Verifies remote-only namespace changes are classified separately from content changes.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteNamespaceChangedWhenOnlyRemoteNameChanged()
    {
        SyncItemState RemoteState = State(Name: "Renamed.txt");
        RemoteState.LocalRelativePath = "Report.txt";

        SyncDiffKind Kind = Classify(State(), State(), RemoteState);

        Assert.Equal(SyncDiffKind.RemoteNamespaceChanged, Kind);
    }
    /// <summary>
    /// Verifies local-only namespace changes are classified separately from content changes.
    /// </summary>
    [Fact]
    public void ClassifyReturnsLocalNamespaceChangedWhenOnlyLocalNameChanged()
    {
        SyncItemState LocalState = State(Name: "Renamed.txt");
        LocalState.LocalRelativePath = "Renamed.txt";

        SyncDiffKind Kind = Classify(State(), LocalState, State());

        Assert.Equal(SyncDiffKind.LocalNamespaceChanged, Kind);
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
    /// Verifies differing local and remote renames are classified as a conflict.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenLocalAndRemoteRenameDiffer()
    {
        SyncItemState LocalState = State(Name: "LocalName.txt");
        LocalState.LocalRelativePath = "LocalName.txt";
        SyncItemState RemoteState = State(Name: "RemoteName.txt");
        RemoteState.LocalRelativePath = "Report.txt";

        SyncDiffKind Kind = Classify(State(), LocalState, RemoteState);

        Assert.Equal(SyncDiffKind.Conflict, Kind);
    }
    /// <summary>
    /// Verifies differing local and remote moves are classified as a conflict.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenLocalAndRemoteMoveDiffer()
    {
        SyncItemState LocalState = State();
        LocalState.LocalRelativePath = "LocalFolder/Report.txt";
        SyncItemState RemoteState = State();
        RemoteState.RemoteParentId = "remote-folder";

        SyncDiffKind Kind = Classify(State(), LocalState, RemoteState);

        Assert.Equal(SyncDiffKind.Conflict, Kind);
    }
    /// <summary>
    /// Verifies local delete versus remote content modification is classified as a conflict.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenLocalMissingAndRemoteModified()
    {
        SyncDiffKind Kind = Classify(State(), State(Exists: false), State(Hash: "hash-remote"));

        Assert.Equal(SyncDiffKind.Conflict, Kind);
    }
    /// <summary>
    /// Verifies remote missing versus local content modification is classified as a conflict.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenRemoteMissingAndLocalModified()
    {
        SyncDiffKind Kind = Classify(State(), State(Hash: "hash-local"), State(Exists: false));

        Assert.Equal(SyncDiffKind.Conflict, Kind);
    }
    /// <summary>
    /// Verifies remote permanent delete versus local content modification is classified as a conflict.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenRemoteRemovedAndLocalModified()
    {
        SyncItemState RemoteState = State(Exists: false);
        RemoteState.Removed = true;

        SyncDiffKind Kind = Classify(State(), State(Hash: "hash-local"), RemoteState);

        Assert.Equal(SyncDiffKind.Conflict, Kind);
    }
    /// <summary>
    /// Verifies remote folder delete tombstone versus modified descendant state is classified as a conflict.
    /// </summary>
    [Fact]
    public void ClassifyReturnsConflictWhenRemoteDescendantRemovedAndLocalDescendantModified()
    {
        SyncItemState BaseState = State(Name: "Nested.txt");
        BaseState.LocalRelativePath = "Folder/Nested.txt";
        BaseState.RemoteParentId = "remote-folder";
        SyncItemState LocalState = State(Name: "Nested.txt", Hash: "hash-local");
        LocalState.LocalRelativePath = "Folder/Nested.txt";
        LocalState.RemoteParentId = "remote-folder";
        SyncItemState RemoteState = State(Name: "Nested.txt", Exists: false);
        RemoteState.RemoteParentId = "remote-folder";
        RemoteState.Removed = true;

        SyncDiffKind Kind = Classify(BaseState, LocalState, RemoteState);

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
    /// Verifies a committed missing base with missing local and trashed remote is treated as reconciled.
    /// </summary>
    [Fact]
    public void ClassifyReturnsNoChangeWhenCommittedMissingItemRemainsTrashedRemotely()
    {
        SyncItemState BaseState = State(Exists: false);
        SyncItemState LocalState = State(Exists: false);
        SyncItemState RemoteState = State();
        RemoteState.Trashed = true;

        SyncDiffKind Kind = Classify(BaseState, LocalState, RemoteState);

        Assert.Equal(SyncDiffKind.NoChange, Kind);
    }
    /// <summary>
    /// Verifies a remote item restored after a committed delete is treated as a remote change.
    /// </summary>
    [Fact]
    public void ClassifyReturnsRemoteChangedWhenCommittedMissingItemIsRestoredRemotely()
    {
        SyncItemState BaseState = State(Exists: false);
        SyncItemState LocalState = State(Exists: false);
        SyncItemState RemoteState = State();

        SyncDiffKind Kind = Classify(BaseState, LocalState, RemoteState);

        Assert.Equal(SyncDiffKind.RemoteChanged, Kind);
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

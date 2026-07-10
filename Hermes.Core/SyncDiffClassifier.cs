// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Classifies three-way synchronization state without producing operations.
/// </summary>
public class SyncDiffClassifier
{
    // ● private

    static bool IsMissing(SyncItemState State) => State == null || !State.Exists;
    static bool SameText(string A, string B) => string.Equals(A ?? string.Empty, B ?? string.Empty, StringComparison.Ordinal);
    static bool SameState(SyncItemState A, SyncItemState B)
    {
        if (A == null || B == null)
            return A == B;

        return A.Exists == B.Exists
            && A.Removed == B.Removed
            && A.Trashed == B.Trashed
            && SameText(A.ItemType, B.ItemType)
            && SameText(A.Name, B.Name)
            && SameText(A.LocalRelativePath, B.LocalRelativePath)
            && SameText(A.RemoteParentId, B.RemoteParentId)
            && SameText(A.ContentHash, B.ContentHash)
            && A.Size == B.Size
            && A.ProviderVersion == B.ProviderVersion;
    }
    static bool HasChanged(SyncItemState BaseState, SyncItemState ObservedState)
    {
        if (BaseState == null)
            return ObservedState != null && ObservedState.Exists;

        return !SameState(BaseState, ObservedState);
    }
    static SyncDiffKind ClassifyWithoutBase(SyncItemState LocalState, SyncItemState RemoteState)
    {
        bool LocalMissing = IsMissing(LocalState);
        bool RemoteMissing = IsMissing(RemoteState);

        if (LocalMissing && RemoteMissing)
            return SyncDiffKind.NoChange;

        if (!LocalMissing && RemoteMissing)
            return SyncDiffKind.LocalChanged;

        if (LocalMissing && !RemoteMissing)
            return SyncDiffKind.RemoteChanged;

        return SameState(LocalState, RemoteState)
            ? SyncDiffKind.BothChangedCompatible
            : SyncDiffKind.Conflict;
    }

    // ● public

    /// <summary>
    /// Classifies the specified three-way synchronization state.
    /// </summary>
    public SyncDiffKind Classify(SyncDiffInput Input)
    {
        Guard.NotNull(Input, nameof(Input));

        if (Input.NamespaceCollisionDetected)
            return SyncDiffKind.NamespaceCollision;

        SyncItemState LocalState = Input.LocalState;
        SyncItemState RemoteState = Input.RemoteState;

        if (RemoteState != null && RemoteState.Removed)
            return SyncDiffKind.RemoteRemoved;

        if (RemoteState != null && RemoteState.Exists && RemoteState.Trashed)
            return SyncDiffKind.RemoteTrashed;

        if (Input.BaseState == null)
            return ClassifyWithoutBase(LocalState, RemoteState);

        bool LocalMissing = IsMissing(LocalState);
        bool RemoteMissing = IsMissing(RemoteState);

        if (LocalMissing && !RemoteMissing)
            return SyncDiffKind.LocalMissing;

        if (RemoteMissing && !LocalMissing)
            return SyncDiffKind.RemoteMissing;

        bool LocalChanged = HasChanged(Input.BaseState, LocalState);
        bool RemoteChanged = HasChanged(Input.BaseState, RemoteState);

        if (!LocalChanged && !RemoteChanged)
            return SyncDiffKind.NoChange;

        if (LocalChanged && !RemoteChanged)
            return SyncDiffKind.LocalChanged;

        if (!LocalChanged && RemoteChanged)
            return SyncDiffKind.RemoteChanged;

        return SameState(LocalState, RemoteState)
            ? SyncDiffKind.BothChangedCompatible
            : SyncDiffKind.Conflict;
    }
}

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
    static bool IsCommittedMissing(SyncItemState State) => State != null && !State.Exists;
    static bool SameText(string A, string B) => string.Equals(A ?? string.Empty, B ?? string.Empty, StringComparison.Ordinal);
    static string ParentLocalPath(string LocalRelativePath)
    {
        if (string.IsNullOrWhiteSpace(LocalRelativePath))
            return string.Empty;

        int Index = LocalRelativePath.LastIndexOf('/');
        return Index < 0 ? string.Empty : LocalRelativePath[..Index];
    }
    static bool SameContent(SyncItemState A, SyncItemState B)
    {
        if (A == null || B == null)
            return A == B;

        return A.Exists == B.Exists
            && A.Removed == B.Removed
            && A.Trashed == B.Trashed
            && SameText(A.ItemType, B.ItemType)
            && SameText(A.ContentHash, B.ContentHash)
            && A.Size == B.Size;
    }
    static bool SameRemoteNamespace(SyncItemState A, SyncItemState B)
    {
        if (A == null || B == null)
            return A == B;

        return SameText(A.Name, B.Name)
            && SameText(A.RemoteParentId, B.RemoteParentId);
    }
    static bool SameLocalNamespace(SyncItemState A, SyncItemState B)
    {
        if (A == null || B == null)
            return A == B;

        return SameText(A.LocalRelativePath, B.LocalRelativePath);
    }
    static bool IsReconciledRemoteRemoval(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return IsCommittedMissing(BaseState)
            && IsMissing(LocalState)
            && RemoteState != null
            && (!RemoteState.Exists || RemoteState.Removed || RemoteState.Trashed);
    }
    static bool IsRemoteRestore(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return IsCommittedMissing(BaseState)
            && IsMissing(LocalState)
            && RemoteState != null
            && RemoteState.Exists
            && !RemoteState.Removed
            && !RemoteState.Trashed;
    }
    static bool IsCompatibleRestore(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return IsCommittedMissing(BaseState)
            && !IsMissing(LocalState)
            && !IsMissing(RemoteState)
            && SameRestoredProjectedState(LocalState, RemoteState);
    }
    static bool IsLocalRestoreAfterRemoteRemoval(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return IsCommittedMissing(BaseState)
            && !IsMissing(LocalState)
            && RemoteState != null
            && (RemoteState.Removed || RemoteState.Trashed || !RemoteState.Exists);
    }
    static bool IsCompatibleEndpointRemoval(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return BaseState != null
            && BaseState.Exists
            && IsMissing(LocalState)
            && RemoteState != null
            && (RemoteState.Removed || RemoteState.Trashed);
    }
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
            && A.Size == B.Size;
    }
    static bool HasChanged(SyncItemState BaseState, SyncItemState ObservedState)
    {
        if (BaseState == null)
            return ObservedState != null && ObservedState.Exists;

        return !SameState(BaseState, ObservedState);
    }
    static bool HasActiveChange(SyncItemState BaseState, SyncItemState ObservedState)
    {
        return !IsMissing(ObservedState) && HasChanged(BaseState, ObservedState);
    }
    static bool HasOnlyRemoteNamespaceChanged(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return BaseState != null
            && !IsMissing(LocalState)
            && !IsMissing(RemoteState)
            && SameState(BaseState, LocalState)
            && SameContent(BaseState, RemoteState)
            && SameLocalNamespace(BaseState, RemoteState)
            && !SameRemoteNamespace(BaseState, RemoteState);
    }
    static bool HasOnlyLocalNamespaceChanged(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return BaseState != null
            && !IsMissing(LocalState)
            && !IsMissing(RemoteState)
            && SameState(BaseState, RemoteState)
            && SameContent(BaseState, LocalState)
            && !SameLocalNamespace(BaseState, LocalState);
    }
    static bool HasCompatibleRename(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return BaseState != null
            && !IsMissing(LocalState)
            && !IsMissing(RemoteState)
            && SameContent(BaseState, LocalState)
            && SameContent(BaseState, RemoteState)
            && SameText(LocalState.Name, RemoteState.Name)
            && SameText(BaseState.RemoteParentId, RemoteState.RemoteParentId)
            && SameText(ParentLocalPath(BaseState.LocalRelativePath), ParentLocalPath(LocalState.LocalRelativePath))
            && !SameText(BaseState.Name, LocalState.Name)
            && !SameText(BaseState.Name, RemoteState.Name);
    }
    static bool HasCompatibleProjectedNamespaceChange(SyncItemState BaseState, SyncItemState LocalState, SyncItemState RemoteState)
    {
        return BaseState != null
            && !IsMissing(LocalState)
            && !IsMissing(RemoteState)
            && SameContent(BaseState, LocalState)
            && SameContent(BaseState, RemoteState)
            && !string.IsNullOrWhiteSpace(RemoteState.ProjectedLocalRelativePath)
            && SameText(LocalState.LocalRelativePath, RemoteState.ProjectedLocalRelativePath)
            && !SameLocalNamespace(BaseState, LocalState)
            && !SameRemoteNamespace(BaseState, RemoteState);
    }
    static bool SameProjectedState(SyncItemState LocalState, SyncItemState RemoteState)
    {
        return SameContent(LocalState, RemoteState)
            && SameText(LocalState.Name, RemoteState.Name)
            && !string.IsNullOrWhiteSpace(RemoteState.ProjectedLocalRelativePath)
            && SameText(LocalState.LocalRelativePath, RemoteState.ProjectedLocalRelativePath);
    }
    static bool SameRestoredProjectedState(SyncItemState LocalState, SyncItemState RemoteState)
    {
        return LocalState != null
            && RemoteState != null
            && LocalState.Exists
            && RemoteState.Exists
            && !RemoteState.Removed
            && !RemoteState.Trashed
            && SameText(LocalState.ItemType, RemoteState.ItemType)
            && SameText(LocalState.Name, RemoteState.Name)
            && SameText(LocalState.ContentHash, RemoteState.ContentHash)
            && LocalState.Size == RemoteState.Size
            && !string.IsNullOrWhiteSpace(RemoteState.ProjectedLocalRelativePath)
            && SameText(LocalState.LocalRelativePath, RemoteState.ProjectedLocalRelativePath);
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

        return SameState(LocalState, RemoteState) || SameProjectedState(LocalState, RemoteState)
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

        if (IsReconciledRemoteRemoval(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.NoChange;

        if (IsCompatibleRestore(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.BothChangedCompatible;

        if (IsLocalRestoreAfterRemoteRemoval(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.LocalChanged;

        if (IsRemoteRestore(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.RemoteChanged;

        if (Input.BaseState == null)
            return ClassifyWithoutBase(LocalState, RemoteState);

        bool LocalChanged = HasChanged(Input.BaseState, LocalState);
        bool RemoteChanged = HasChanged(Input.BaseState, RemoteState);

        if (IsCompatibleEndpointRemoval(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.BothChangedCompatible;

        if (RemoteState != null && RemoteState.Removed)
            return HasActiveChange(Input.BaseState, LocalState)
                ? SyncDiffKind.Conflict
                : SyncDiffKind.RemoteRemoved;

        if (RemoteState != null && RemoteState.Exists && RemoteState.Trashed)
            return HasActiveChange(Input.BaseState, LocalState)
                ? SyncDiffKind.Conflict
                : SyncDiffKind.RemoteTrashed;

        if (HasCompatibleRename(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.BothChangedCompatible;

        if (HasCompatibleProjectedNamespaceChange(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.BothChangedCompatible;

        if (HasOnlyRemoteNamespaceChanged(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.RemoteNamespaceChanged;

        if (HasOnlyLocalNamespaceChanged(Input.BaseState, LocalState, RemoteState))
            return SyncDiffKind.LocalNamespaceChanged;

        bool LocalMissing = IsMissing(LocalState);
        bool RemoteMissing = IsMissing(RemoteState);

        if (LocalMissing && !RemoteMissing)
            return RemoteChanged
                ? SyncDiffKind.Conflict
                : SyncDiffKind.LocalMissing;

        if (RemoteMissing && !LocalMissing)
            return LocalChanged
                ? SyncDiffKind.Conflict
                : SyncDiffKind.RemoteMissing;

        if (LocalMissing)
            return SyncDiffKind.LocalMissing;

        if (!LocalChanged && !RemoteChanged)
            return SyncDiffKind.NoChange;

        if (LocalChanged && !RemoteChanged)
            return SyncDiffKind.LocalChanged;

        if (!LocalChanged && RemoteChanged)
            return SyncDiffKind.RemoteChanged;

        return SameState(LocalState, RemoteState) || SameProjectedState(LocalState, RemoteState)
            ? SyncDiffKind.BothChangedCompatible
            : SyncDiffKind.Conflict;
    }
}

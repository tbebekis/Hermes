// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Maps persisted metadata records to in-memory synchronization state.
/// </summary>
static public class SyncItemStateMapper
{
    // ● private

    static long Value(long? Value) => Value ?? 0;
    static bool Value(bool? Value) => Value ?? false;
    static SyncItemState EmptyMissingState() => new()
    {
        Exists = false,
    };

    // ● public

    /// <summary>
    /// Maps a base snapshot record to synchronization item state.
    /// </summary>
    static public SyncItemState FromBaseSnapshot(BaseSnapshotRecord Record)
    {
        if (Record == null)
            return null;

        return new SyncItemState()
        {
            Exists = Record.ExistsFlag,
            ItemType = Record.ItemType,
            Name = Record.Name,
            LocalRelativePath = Record.LocalRelativePath,
            RemoteParentId = Record.RemoteParentId,
            ContentHash = Record.ContentHash,
            Size = Value(Record.Size),
            ProviderVersion = Value(Record.ProviderVersion),
            Trashed = Value(Record.Trashed),
        };
    }
    /// <summary>
    /// Maps a local observation record to synchronization item state.
    /// </summary>
    static public SyncItemState FromLocalObservation(LocalObservedSnapshotRecord Record)
    {
        if (Record == null)
            return null;

        return new SyncItemState()
        {
            Exists = Record.ExistsFlag,
            ItemType = Record.ItemType,
            Name = Record.Name,
            LocalRelativePath = Record.RelativePath,
            ContentHash = Record.ContentHash,
            Size = Value(Record.Size),
        };
    }
    /// <summary>
    /// Maps a remote observation record to synchronization item state.
    /// </summary>
    static public SyncItemState FromRemoteObservation(RemoteObservedSnapshotRecord Record)
    {
        if (Record == null)
            return null;

        return new SyncItemState()
        {
            Exists = Record.ExistsFlag,
            Removed = Record.Removed,
            Trashed = Value(Record.Trashed),
            ItemType = Record.ItemType,
            Name = Record.Name,
            RemoteParentId = Record.RemoteParentId,
            ContentHash = Record.ContentHash,
            Size = Value(Record.Size),
            ProviderVersion = Value(Record.ProviderVersion),
        };
    }
    /// <summary>
    /// Creates classifier input from persisted metadata records.
    /// </summary>
    static public SyncDiffInput CreateDiffInput(
        BaseSnapshotRecord BaseSnapshot,
        LocalObservedSnapshotRecord LocalObservation,
        RemoteObservedSnapshotRecord RemoteObservation,
        bool NamespaceCollisionDetected = false,
        string RemoteProjectedLocalRelativePath = null)
    {
        SyncItemState BaseState = FromBaseSnapshot(BaseSnapshot);
        SyncItemState LocalState = FromLocalObservation(LocalObservation);
        SyncItemState RemoteState = FromRemoteObservation(RemoteObservation);

        if (LocalObservation != null && !LocalObservation.ExistsFlag)
            LocalState = EmptyMissingState();

        if (RemoteObservation != null && !RemoteObservation.ExistsFlag)
            RemoteState = EmptyMissingState();

        if (LocalState != null && BaseState != null)
        {
            LocalState.RemoteParentId = BaseState.RemoteParentId;
            LocalState.ProviderVersion = BaseState.ProviderVersion;
            LocalState.Trashed = BaseState.Trashed;
        }

        if (RemoteState != null && BaseState != null)
            RemoteState.LocalRelativePath = BaseState.LocalRelativePath;

        if (RemoteState != null)
            RemoteState.ProjectedLocalRelativePath = RemoteProjectedLocalRelativePath;

        if (RemoteObservation != null && RemoteObservation.Removed)
        {
            RemoteState ??= EmptyMissingState();
            RemoteState.Removed = true;
        }

        return new SyncDiffInput()
        {
            BaseState = BaseState,
            LocalState = LocalState,
            RemoteState = RemoteState,
            NamespaceCollisionDetected = NamespaceCollisionDetected,
        };
    }
}

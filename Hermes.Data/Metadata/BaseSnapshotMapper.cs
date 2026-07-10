// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Maps verified endpoint observations to committed base snapshots.
/// </summary>
static public class BaseSnapshotMapper
{
    // ● private

    static void CheckTrackedItem(LocalObservedSnapshotRecord LocalObservation, RemoteObservedSnapshotRecord RemoteObservation)
    {
        if (!string.Equals(LocalObservation.TrackedItemId, RemoteObservation.TrackedItemId, StringComparison.Ordinal))
            throw new ArgumentException("Local and remote observations must belong to the same tracked item.");
    }

    // ● public

    /// <summary>
    /// Creates a committed base snapshot from verified local and remote observations.
    /// </summary>
    static public BaseSnapshotRecord FromVerifiedObservations(LocalObservedSnapshotRecord LocalObservation, RemoteObservedSnapshotRecord RemoteObservation, DateTime CommittedTime)
    {
        Guard.NotNull(LocalObservation, nameof(LocalObservation));
        Guard.NotNull(RemoteObservation, nameof(RemoteObservation));
        CheckTrackedItem(LocalObservation, RemoteObservation);

        if (!LocalObservation.ExistsFlag || !RemoteObservation.ExistsFlag || RemoteObservation.Removed)
        {
            return new BaseSnapshotRecord()
            {
                TrackedItemId = LocalObservation.TrackedItemId,
                ExistsFlag = false,
                CommittedTime = CommittedTime,
            };
        }

        return new BaseSnapshotRecord()
        {
            TrackedItemId = LocalObservation.TrackedItemId,
            ExistsFlag = true,
            ItemType = RemoteObservation.ItemType ?? LocalObservation.ItemType,
            Name = RemoteObservation.Name ?? LocalObservation.Name,
            LocalRelativePath = LocalObservation.RelativePath,
            RemoteParentId = RemoteObservation.RemoteParentId,
            Size = RemoteObservation.Size ?? LocalObservation.Size,
            ContentHash = RemoteObservation.ContentHash ?? LocalObservation.ContentHash,
            CreatedTime = RemoteObservation.CreatedTime,
            ModifiedTime = RemoteObservation.ModifiedTime,
            ProviderVersion = RemoteObservation.ProviderVersion,
            Trashed = RemoteObservation.Trashed,
            CommittedTime = CommittedTime,
        };
    }
}

// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests open conflict API response mapping.
/// </summary>
public class OpenConflictResponseTests
{
    // ● public

    /// <summary>
    /// Verifies conflict detail records map to compact response items.
    /// </summary>
    [Fact]
    public void FromDetailMapsConflictContext()
    {
        DateTime Time = new(2026, 7, 11, 13, 0, 0, DateTimeKind.Utc);
        SyncConflictDetailRecord Detail = new()
        {
            Conflict = new SyncConflictRecord()
            {
                Id = "conflict-1",
                TrackedItemId = "item-1",
                DiffKind = SyncDiffKind.Conflict,
                DecisionKind = SyncPlanDecisionKind.Conflict,
                Message = "Both sides changed.",
                LastObservedTime = Time,
            },
            LocalObservation = new LocalObservedSnapshotRecord()
            {
                RelativePath = "Local/File.txt",
            },
            RemoteObservation = new RemoteObservedSnapshotRecord()
            {
                Name = "RemoteFile.txt",
            },
        };

        OpenConflictResponse Response = OpenConflictResponse.FromDetail(Detail);

        Assert.Equal("conflict-1", Response.Id);
        Assert.Equal("item-1", Response.TrackedItemId);
        Assert.Equal("Conflict", Response.DiffKind);
        Assert.Equal("Conflict", Response.DecisionKind);
        Assert.Equal("Both sides changed.", Response.Message);
        Assert.Equal("Local/File.txt", Response.LocalPath);
        Assert.Equal("RemoteFile.txt", Response.RemoteName);
        Assert.Equal(Time, Response.LastObservedTime);
    }
}

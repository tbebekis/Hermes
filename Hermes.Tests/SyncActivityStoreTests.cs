// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization activity storage.
/// </summary>
public class SyncActivityStoreTests
{
    // ● private

    static MetadataSyncRunResult RunResult()
    {
        MetadataSyncSessionResult SessionResult = new();
        SyncExecutionRequest Request = new()
        {
            Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalChanged, SyncPlanDecisionKind.UploadToRemote),
        };
        SessionResult.PendingExecutionRequests.Add(Request);
        SyncExecutionApplyResult ApplyResult = new();
        ApplyResult.CommittedResults.Add(SyncExecutionResultFactory.Completed(Request));

        return new MetadataSyncRunResult()
        {
            Kind = MetadataSyncRunKind.Incremental,
            LocalObservedItemCount = 1,
            RemoteObservedChangeCount = 0,
            SessionResult = SessionResult,
            ExecutionApplyResult = ApplyResult,
        };
    }

    // ● public

    /// <summary>
    /// Verifies successful sync pass activity is stored.
    /// </summary>
    [Fact]
    public void AddSuccessStoresActivity()
    {
        SyncActivityStore Store = new();

        Store.AddSuccess("default", RunResult());

        SyncActivityRecord Record = Assert.Single(Store.GetRecent());
        Assert.Equal("Information", Record.Level);
        Assert.Equal("default", Record.SyncRootId);
        Assert.Equal("Sync pass completed", Record.Title);
        Assert.Contains("Pending executions: 1", Record.Details);
        Assert.Contains("Pending summary: UploadToRemote=1", Record.Details);
        Assert.Contains("Committed executions: 1", Record.Details);
        Assert.Contains("Uncommitted executions: 0", Record.Details);
    }
    /// <summary>
    /// Verifies activity response maps records.
    /// </summary>
    [Fact]
    public void ResponseMapsRecord()
    {
        SyncActivityRecord Record = new()
        {
            TimestampUtc = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
            Level = "Error",
            SyncRootId = "default",
            Title = "Sync pass failed",
            Details = "Provider error.",
        };

        SyncActivityResponse Response = SyncActivityResponse.FromRecord(Record);

        Assert.Equal(Record.TimestampUtc, Response.TimestampUtc);
        Assert.Equal("Error", Response.Level);
        Assert.Equal("default", Response.SyncRootId);
        Assert.Equal("Sync pass failed", Response.Title);
        Assert.Equal("Provider error.", Response.Details);
    }
}

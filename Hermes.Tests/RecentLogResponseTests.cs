// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests recent log API response mapping.
/// </summary>
public class RecentLogResponseTests
{
    // ● public

    /// <summary>
    /// Verifies recent log records map to response items.
    /// </summary>
    [Fact]
    public void FromRecordMapsLogFields()
    {
        RecentLogRecord Record = new()
        {
            Id = "log-1",
            LogTime = "26-07-11 14:00:00",
            Level = "Info",
            Source = "Hermes",
            EventId = "event-1",
            Message = "Started.",
        };

        RecentLogResponse Response = RecentLogResponse.FromRecord(Record);

        Assert.Equal("log-1", Response.Id);
        Assert.Equal("26-07-11 14:00:00", Response.LogTime);
        Assert.Equal("Info", Response.Level);
        Assert.Equal("Hermes", Response.Source);
        Assert.Equal("event-1", Response.EventId);
        Assert.Equal("Started.", Response.Message);
    }
}

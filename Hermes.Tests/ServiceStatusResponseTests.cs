// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests local service status response creation.
/// </summary>
public class ServiceStatusResponseTests
{
    // ● private

    static SyncRootRecord SyncRoot() => new()
    {
        Id = "default",
        ProviderName = "GoogleDrive",
        ConnectionId = "account-1",
        LocalRootPath = "/tmp/hermes",
        RemoteRootItemId = "root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
    };
    static SyncSettings Settings() => new()
    {
        SyncRootId = "default",
        LocalRootPath = "/tmp/hermes",
        RemoteRootFolderId = "root",
        PollingIntervalSeconds = 60,
        EnableMutations = true,
    };

    // ● public

    /// <summary>
    /// Verifies the service status response exposes the current process snapshot.
    /// </summary>
    [Fact]
    public void CreateReturnsCurrentServiceStatus()
    {
        ServiceStatusResponse Response = ServiceStatusResponse.Create(SyncRoot(), Settings(), 3);

        Assert.Equal("Running", Response.ServiceStatus);
        Assert.Equal("Unknown", Response.SynchronizationStatus);
        Assert.Equal("Connected", Response.IpcStatus);
        Assert.Equal(Environment.ProcessId, Response.ProcessId);
        Assert.False(string.IsNullOrWhiteSpace(Response.Version));
        Assert.True(Response.StartedUtc <= DateTime.UtcNow);
        Assert.True(Response.UptimeSeconds >= 0);
        Assert.Equal("default", Response.SyncRootId);
        Assert.Equal("GoogleDrive", Response.ProviderName);
        Assert.Equal("/tmp/hermes", Response.LocalRootPath);
        Assert.Equal("root", Response.RemoteRootItemId);
        Assert.True(Response.SyncRootEnabled);
        Assert.True(Response.MutationsEnabled);
        Assert.Equal(60, Response.PollingIntervalSeconds);
        Assert.Equal(3, Response.OpenConflictCount);
        Assert.True(Response.TimestampUtc <= DateTime.UtcNow);
    }
}

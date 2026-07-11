// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests local service status response creation.
/// </summary>
public class ServiceStatusResponseTests
{
    // ● public

    /// <summary>
    /// Verifies the service status response exposes the current process snapshot.
    /// </summary>
    [Fact]
    public void CreateReturnsCurrentServiceStatus()
    {
        ServiceStatusResponse Response = ServiceStatusResponse.Create();

        Assert.Equal("Running", Response.ServiceStatus);
        Assert.Equal("Unknown", Response.SynchronizationStatus);
        Assert.Equal("Connected", Response.IpcStatus);
        Assert.Equal(Environment.ProcessId, Response.ProcessId);
        Assert.False(string.IsNullOrWhiteSpace(Response.Version));
        Assert.True(Response.TimestampUtc <= DateTime.UtcNow);
    }
}

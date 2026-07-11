// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests service control response mapping.
/// </summary>
public class ServiceControlResponseTests
{
    // ● public

    /// <summary>
    /// Verifies successful control responses include command result data.
    /// </summary>
    [Fact]
    public void SuccessCreatesResponse()
    {
        ServiceControlResponse Response = ServiceControlResponse.Success("Stop requested.");

        Assert.True(Response.Succeeded);
        Assert.Equal("Stop requested.", Response.Message);
        Assert.True(Response.TimestampUtc <= DateTime.UtcNow);
    }
}

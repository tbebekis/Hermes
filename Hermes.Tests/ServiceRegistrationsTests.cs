// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests for Hermes service dependency registrations.
/// </summary>
public class ServiceRegistrationsTests
{
    // ● private

    static IConfiguration CreateConfiguration()
    {
        Dictionary<string, string> Values = new()
        {
            ["Sync:SyncRootId"] = "default",
            ["Sync:LocalRootPath"] = "/tmp/hermes",
            ["Sync:RemoteRootFolderId"] = "root",
            ["Sync:PollingIntervalSeconds"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(Values)
            .Build();
    }

    // ● public

    /// <summary>
    /// Ensures the metadata sync pipeline registrations can be added without the hosted worker.
    /// </summary>
    [Fact]
    public void AddHermesServiceServicesCanSkipWorker()
    {
        ServiceCollection Services = new();

        Services.AddHermesServiceServices(CreateConfiguration(), false);

        Assert.Contains(Services, Item => Item.ServiceType == typeof(IMetadataSyncRunner) && Item.ImplementationType == typeof(MetadataSyncRunner));
        Assert.Contains(Services, Item => Item.ServiceType == typeof(MetadataSyncLoop));
        Assert.Contains(Services, Item => Item.ServiceType == typeof(SyncRootRecord));
        Assert.DoesNotContain(Services, Item => Item.ServiceType == typeof(IHostedService));
    }
    /// <summary>
    /// Ensures the hosted worker is registered by default.
    /// </summary>
    [Fact]
    public void AddHermesServiceServicesRegistersWorkerByDefault()
    {
        ServiceCollection Services = new();

        Services.AddHermesServiceServices(CreateConfiguration());

        Assert.Contains(Services, Item => Item.ServiceType == typeof(IHostedService));
    }
}

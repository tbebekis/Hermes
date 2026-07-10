// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests for Hermes service dependency registrations.
/// </summary>
public class ServiceRegistrationsTests
{
    // ● private

    static IConfiguration CreateConfiguration(Dictionary<string, string> Values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(Values)
            .Build();
    }
    static IConfiguration CreateConfiguration()
    {
        Dictionary<string, string> Values = new()
        {
            ["Sync:SyncRootId"] = "default",
            ["Sync:LocalRootPath"] = "/tmp/hermes",
            ["Sync:RemoteRootFolderId"] = "root",
            ["Sync:PollingIntervalSeconds"] = "60",
            ["Sync:EnableMutations"] = "false"
        };

        return CreateConfiguration(Values);
    }
    static IConfiguration CreateInvalidConfiguration()
    {
        Dictionary<string, string> Values = new()
        {
            ["Sync:SyncRootId"] = "",
            ["Sync:LocalRootPath"] = "",
            ["Sync:RemoteRootFolderId"] = "",
            ["Sync:PollingIntervalSeconds"] = "0",
            ["Sync:EnableMutations"] = "false"
        };

        return CreateConfiguration(Values);
    }
    static IConfiguration CreateMutationConfiguration(bool Enabled)
    {
        Dictionary<string, string> Values = new()
        {
            ["Sync:SyncRootId"] = "default",
            ["Sync:LocalRootPath"] = "/tmp/hermes",
            ["Sync:RemoteRootFolderId"] = "root",
            ["Sync:PollingIntervalSeconds"] = "60",
            ["Sync:EnableMutations"] = Enabled ? "true" : "false"
        };

        return CreateConfiguration(Values);
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
    /// Ensures the service graph can be validated without starting the worker.
    /// </summary>
    [Fact]
    public void AddHermesServiceServicesBuildsValidatedProviderWithoutWorker()
    {
        ServiceCollection Services = new();

        Services.AddHermesServiceServices(CreateConfiguration(), false);

        using ServiceProvider Provider = Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.NotNull(Provider.GetRequiredService<IOptions<SyncSettings>>());
    }
    /// <summary>
    /// Ensures invalid synchronization settings fail through the registered options path.
    /// </summary>
    [Fact]
    public void AddHermesServiceServicesValidatesSyncSettings()
    {
        ServiceCollection Services = new();

        Services.AddHermesServiceServices(CreateInvalidConfiguration(), false);

        using ServiceProvider Provider = Services.BuildServiceProvider();
        OptionsValidationException Ex = Assert.Throws<OptionsValidationException>(() =>
            Provider.GetRequiredService<IOptions<SyncSettings>>().Value);

        Assert.Contains("SyncRootId is required.", Ex.Failures);
        Assert.Contains("LocalRootPath is required.", Ex.Failures);
        Assert.Contains("RemoteRootFolderId is required.", Ex.Failures);
        Assert.Contains("PollingIntervalSeconds must be greater than zero.", Ex.Failures);
    }
    /// <summary>
    /// Ensures disabled mutations use the non-mutating executor.
    /// </summary>
    [Fact]
    public void AddHermesServiceServicesUsesUnsupportedExecutorWhenMutationsAreDisabled()
    {
        ServiceCollection Services = new();

        Services.AddHermesServiceServices(CreateMutationConfiguration(false), false);

        using ServiceProvider Provider = Services.BuildServiceProvider();
        ISyncExecutor Executor = Provider.GetRequiredService<ISyncExecutor>();

        Assert.IsType<UnsupportedSyncExecutor>(Executor);
    }
    /// <summary>
    /// Ensures enabled mutations use the endpoint mutation executor.
    /// </summary>
    [Fact]
    public void AddHermesServiceServicesUsesMutationExecutorWhenMutationsAreEnabled()
    {
        ServiceCollection Services = new();

        Services.AddHermesServiceServices(CreateMutationConfiguration(true), false);
        Services.AddSingleton(MetadataSyncTestHost.CreateSyncRoot());

        using ServiceProvider Provider = Services.BuildServiceProvider();
        ISyncExecutor Executor = Provider.GetRequiredService<ISyncExecutor>();

        Assert.IsType<SyncMutationExecutorBase>(Executor);
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

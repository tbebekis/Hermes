// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Registers Hermes service dependencies.
/// </summary>
static public class ServiceRegistrations
{
    // ● public

    /// <summary>
    /// Registers the dependencies used by the Hermes service application.
    /// </summary>
    static public IServiceCollection AddHermesServiceServices(this IServiceCollection Services, IConfiguration Configuration, bool AddWorker = true)
    {
        Guard.NotNull(Services, nameof(Services));
        Guard.NotNull(Configuration, nameof(Configuration));

        Services.AddOptions<SyncSettings>()
            .Bind(Configuration.GetSection("Sync"))
            .ValidateOnStart();
        Services.AddSingleton<IValidateOptions<SyncSettings>, SyncSettingsValidator>();
        Services.Configure<GoogleDriveSettings>(Configuration.GetSection("GoogleDrive"));
        Services.AddSingleton<GoogleDriveAuthManager>();
        Services.AddSingleton<GoogleDriveClient>();
        Services.AddSingleton<GoogleDriveMapper>();
        Services.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
        Services.AddSingleton<ILocalSyncMutationEndpoint>(Provider =>
        {
            SyncRootRecord SyncRoot = Provider.GetRequiredService<SyncRootRecord>();
            return new LocalSyncMutationEndpoint(SyncRoot.LocalRootPath);
        });
        Services.AddSingleton<IRemoteSyncMutationEndpoint, GoogleDriveRemoteSyncMutationEndpoint>();
        Services.AddSingleton<ISyncExecutor, SyncMutationExecutorBase>();
        Services.AddSingleton(_ => ServiceDataStartup.CreateDefaultStore());
        Services.AddSingleton<SqlMetadataStore>();
        Services.AddSingleton(Provider =>
        {
            SqlMetadataStore Store = Provider.GetRequiredService<SqlMetadataStore>();
            SyncSettings Settings = Provider.GetRequiredService<IOptions<SyncSettings>>().Value;
            return SyncRootSettingsSynchronizer.EnsureSyncRoot(Store, Settings, DateTime.UtcNow);
        });
        Services.AddSingleton<MetadataSyncSession>();
        Services.AddSingleton<IMetadataSyncRunner, MetadataSyncRunner>();
        Services.AddSingleton<MetadataSyncLoop>();
        Services.AddSingleton<SyncPlanner>();
        Services.AddSingleton<LocalScanner>();

        if (AddWorker)
            Services.AddHostedService<Worker>();

        return Services;
    }
}

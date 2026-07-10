// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Service application entry point.
/// </summary>
static public class Program
{
    // ● public

    /// <summary>
    /// Runs the Hermes service.
    /// </summary>
    static public async Task Main(string[] Args)
    {
        IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(Args)
            .UseSystemd()
            .ConfigureServices((Context, Services) =>
            {
                Services.AddOptions<SyncSettings>()
                    .Bind(Context.Configuration.GetSection("Sync"))
                    .ValidateOnStart();
                Services.AddSingleton<IValidateOptions<SyncSettings>, SyncSettingsValidator>();
                Services.Configure<GoogleDriveSettings>(Context.Configuration.GetSection("GoogleDrive"));
                Services.AddSingleton<GoogleDriveAuthManager>();
                Services.AddSingleton<GoogleDriveClient>();
                Services.AddSingleton<GoogleDriveMapper>();
                Services.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
                Services.AddSingleton<ILocalSyncMutationEndpoint>(Provider =>
                {
                    SyncSettings Settings = Provider.GetRequiredService<IOptions<SyncSettings>>().Value;
                    return new LocalSyncMutationEndpoint(Settings.LocalRootPath);
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
                Services.AddSingleton<MetadataSyncRunner>();
                Services.AddSingleton<MetadataSyncLoop>();
                Services.AddSingleton<SyncPlanner>();
                Services.AddSingleton<LocalScanner>();
                Services.AddHostedService<Worker>();
            })
            .Build();

        await Host.RunAsync();
    }
}

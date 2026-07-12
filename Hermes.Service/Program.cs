// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Service application entry point.
/// </summary>
static public class Program
{
    // ● private

    static void MapServiceApi(IEndpointRouteBuilder Endpoints)
    {
        Endpoints.MapGet("/status", (SyncRootRecord SyncRoot, IOptions<SyncSettings> Settings, SqlMetadataStore Store, SyncCycleCoordinator Coordinator) =>
        {
            int OpenConflictCount = Store.CountOpenConflicts(SyncRoot.Id);
            return ServiceStatusResponse.Create(SyncRoot, Settings.Value, OpenConflictCount, Coordinator.IsRunning);
        });
        Endpoints.MapGet("/conflicts/open", (SyncRootRecord SyncRoot, SqlMetadataStore Store) =>
        {
            List<OpenConflictResponse> Result = new();

            foreach (SyncConflictDetailRecord Detail in Store.GetOpenConflictDetails(SyncRoot.Id))
                Result.Add(OpenConflictResponse.FromDetail(Detail));

            return Result;
        });
        Endpoints.MapGet("/logs/recent", (SqlMetadataStore Store) =>
        {
            List<RecentLogResponse> Result = new();

            foreach (RecentLogRecord Record in Store.GetRecentLogs(200))
                Result.Add(RecentLogResponse.FromRecord(Record));

            return Result;
        });
        Endpoints.MapGet("/activity/recent", (SyncActivityStore Store) =>
        {
            List<SyncActivityResponse> Result = new();

            foreach (SyncActivityRecord Record in Store.GetRecent())
                Result.Add(SyncActivityResponse.FromRecord(Record));

            return Result;
        });
        Endpoints.MapPost("/activity/clear", (SyncActivityStore Store) =>
        {
            Store.Clear();
            return ServiceControlResponse.Success("Activity cleared.");
        });
        Endpoints.MapPost("/sync/run-once", async (SyncRootRecord SyncRoot, SyncCycleCoordinator Coordinator, SyncActivityStore ActivityStore, CancellationToken CancellationToken) =>
        {
            ActivityStore.AddInformation(SyncRoot.Id, "Manual sync requested", "A manual synchronization cycle was requested.");
            Result<MetadataSyncRunResult> Result = await Coordinator.TryRunOnceAsync(CancellationToken);

            if (Result.Failed)
            {
                if (Result.ErrorText == SyncCycleCoordinator.SyncAlreadyRunningMessage)
                    ActivityStore.AddWarning(SyncRoot.Id, "Manual sync skipped", Result.ErrorText);
                else
                    ActivityStore.AddFailure(SyncRoot.Id, Result.ErrorText);

                return ServiceControlResponse.Failure(Result.ErrorText);
            }

            ActivityStore.AddSuccess(SyncRoot.Id, Result.Value);
            return ServiceControlResponse.Success("Manual synchronization cycle completed.");
        });
        Endpoints.MapPost("/control/stop", (IHostApplicationLifetime Lifetime) =>
        {
            Task.Run(async () =>
            {
                await Task.Delay(150);
                Lifetime.StopApplication();
            });

            return ServiceControlResponse.Success("Stop requested.");
        });
    }

    // ● public

    /// <summary>
    /// Runs the Hermes service.
    /// </summary>
    static public async Task Main(string[] Args)
    {
        IHost Host = CreateHostBuilder(Args).Build();

        await Host.RunAsync();
    }

    /// <summary>
    /// Creates the Hermes service host builder.
    /// </summary>
    static public IHostBuilder CreateHostBuilder(string[] Args)
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(Args)
            .UseSystemd()
            .ConfigureWebHostDefaults(WebBuilder =>
            {
                WebBuilder.UseUrls("http://127.0.0.1:8765");
                WebBuilder.Configure(App =>
                {
                    App.UseRouting();
                    App.UseEndpoints(MapServiceApi);
                });
            })
            .ConfigureServices((Context, Services) =>
            {
                Services.AddHermesServiceServices(Context.Configuration);
            });
    }
}

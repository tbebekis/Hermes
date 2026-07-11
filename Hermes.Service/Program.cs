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
        Endpoints.MapGet("/status", (SyncRootRecord SyncRoot, IOptions<SyncSettings> Settings, SqlMetadataStore Store) =>
        {
            int OpenConflictCount = Store.CountOpenConflicts(SyncRoot.Id);
            return ServiceStatusResponse.Create(SyncRoot, Settings.Value, OpenConflictCount);
        });
        Endpoints.MapGet("/conflicts/open", (SyncRootRecord SyncRoot, SqlMetadataStore Store) =>
        {
            List<OpenConflictResponse> Result = new();

            foreach (SyncConflictDetailRecord Detail in Store.GetOpenConflictDetails(SyncRoot.Id))
                Result.Add(OpenConflictResponse.FromDetail(Detail));

            return Result;
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

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
            .ConfigureServices((Context, Services) =>
            {
                Services.AddHermesServiceServices(Context.Configuration);
            });
    }
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.DocumentDb;

namespace Shiny.Locations.NorthAmerica;


/// <summary>
/// Runs the <see cref="NorthAmericaSeeder"/> when the Shiny host starts. Document seeders normally run from an
/// <c>IHostedService</c>, which MAUI never starts - this goes through <see cref="IShinyStartupTask"/> instead so it
/// runs in MAUI and native hosts alike. Run-once: the seeder's marker is recorded in the store, so later launches
/// only read it.
/// </summary>
sealed class NorthAmericaSeedTask(
    IServiceProvider services,
    string? storeName,
    ILogger<NorthAmericaSeedTask> logger
) : IShinyStartupTask
{
    public async void Start()
    {
        try
        {
            var store = storeName == null
                ? services.GetRequiredService<IDocumentStore>()
                : services.GetRequiredKeyedService<IDocumentStore>(storeName);

            var applied = await DocumentSeedRunner
                .RunAsync(store, [new NorthAmericaSeeder()])
                .ConfigureAwait(false);

            if (applied.Count > 0)
                logger.LogInformation("Seeded the North American region dataset");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed the North American region dataset");
        }
    }
}

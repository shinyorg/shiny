using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.DocumentDb;

namespace Shiny.Locations.NorthAmerica;

/// <summary>
/// Seeds the embedded reference geographic dataset (<see cref="NorthAmericaData"/>) — US states, Canadian
/// provinces, and US &amp; Canadian cities — into a document store. Provider-agnostic and idempotent:
/// every record is upserted on a deterministic id, so a re-run (after a version bump) converges cleanly.
/// </summary>
public sealed class NorthAmericaSeeder : IDocumentSeeder
{
    /// <summary>The stable seeder name, also used as the run-once marker id.</summary>
    public const string SeederName = "shiny-geofencing-northamerica";

    /// <inheritdoc />
    public string Name => SeederName;

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public async Task SeedAsync(IDocumentStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);

        await store.BatchUpsert(NorthAmericaData.Regions, cancellationToken: cancellationToken).ConfigureAwait(false);
        await store.BatchUpsert(NorthAmericaData.Cities, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

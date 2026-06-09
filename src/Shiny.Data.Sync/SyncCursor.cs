using System;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Data.Sync;


/// <summary>
/// Persistent cursor recording where the last successful inbox pull left off for an endpoint.
/// One row per endpoint key.
/// </summary>
/// <param name="Identifier">The endpoint key this cursor belongs to.</param>
/// <param name="Cursor">The opaque server-supplied cursor (or last ETag / timestamp).</param>
/// <param name="LastPulledAt">When the last pull completed.</param>
public record SyncCursor(
    string Identifier,
    string? Cursor,
    DateTimeOffset LastPulledAt
) : IRepositoryEntity;


/// <summary>
/// Persistent cursor recording where the last successful tombstone fetch left off for an endpoint.
/// Tracked separately from <see cref="SyncCursor"/> because servers usually expose different
/// cursors for the main delta stream vs the tombstone stream. One row per endpoint key.
/// </summary>
/// <param name="Identifier">The endpoint key this tombstone cursor belongs to.</param>
/// <param name="Cursor">The opaque server-supplied cursor.</param>
/// <param name="LastFetchedAt">When the last tombstone fetch completed.</param>
public record SyncTombstoneCursor(
    string Identifier,
    string? Cursor,
    DateTimeOffset LastFetchedAt
) : IRepositoryEntity;

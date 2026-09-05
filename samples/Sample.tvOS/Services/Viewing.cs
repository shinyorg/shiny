using Shiny.Data.Sync;

namespace Sample.tvOS.Services;


/// <summary>
/// What an Apple TV app most plausibly syncs: what was watched, and how far in.
/// </summary>
public record Viewing(
    string Identifier,
    string Title,
    int PositionSeconds,
    DateTimeOffset WatchedAt
) : ISyncEntity;

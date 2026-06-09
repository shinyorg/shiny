using Shiny.Data.Sync.Infrastructure;

namespace Shiny.Data.Sync.Tests;


/// <summary>
/// Thin wrappers over the internal parsers so the test fixture stays focused on call shapes.
/// </summary>
internal static class PublicSurface
{
    public static SyncPullResult ParsePull(string? body) => PullParser.Parse(body);

    public static SyncTombstoneResult ParseTombstone(string? body, string? fallbackCursor = null)
        => TombstoneParser.Parse(body, fallbackCursor);

    public static SyncBatchResult ParseBatch(int outerStatus, System.Collections.Generic.IReadOnlyList<SyncOperation> ops, string? body)
        => BatchResultParser.Parse(outerStatus, ops, body);
}

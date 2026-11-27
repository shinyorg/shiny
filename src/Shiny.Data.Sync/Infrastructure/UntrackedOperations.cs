using System.Collections.Generic;
using System.Linq;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Finds outbox ops that are queued but have nothing driving them. On Apple an op is sent by a native
/// background upload task; an op waiting out a retry carries <see cref="SyncOperation.NextAttemptAt"/>.
/// Older versions could leave an op <c>Pending</c> / <c>InProgress</c> with neither (its task gone, no retry
/// marker), and nothing ever sent it again.
/// </summary>
internal static class UntrackedOperations
{
    /// <summary>
    /// Ops in <see cref="SyncOperationState.Pending"/> or <see cref="SyncOperationState.InProgress"/> with no retry
    /// marker whose identifier is not in <paramref name="trackedIdentifiers"/> (the native tasks, plus anything this
    /// process has already started). Such an op can't be in flight, so re-sending it can't duplicate a request.
    /// Returned in creation order.
    /// </summary>
    public static IReadOnlyList<SyncOperation> Select(IEnumerable<SyncOperation> ops, IReadOnlySet<string> trackedIdentifiers)
        => ops
            .Where(x =>
                x.NextAttemptAt == null &&
                (x.State == SyncOperationState.Pending || x.State == SyncOperationState.InProgress) &&
                !trackedIdentifiers.Contains(x.Identifier)
            )
            .OrderBy(x => x.CreatedAt)
            .ToList();
}

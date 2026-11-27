using System.Collections.Generic;
using System.Linq;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Coalesces a sequence of outbox operations for a single endpoint into the minimum
/// equivalent set that produces the same final server-side state. Rules:
/// <list type="bullet">
///   <item>A <c>Create</c> that was never sent (<c>Attempts == 0</c>, <c>Pending</c>) followed by a <c>Delete</c>
///   cancels out - the entity never existed on the server, so nothing is sent for it.</item>
///   <item>Otherwise a trailing <c>Delete</c> wins — preceding Create/Update for the same entity drop.</item>
///   <item>Create + Update(s) → single <c>Create</c> with the latest payload.</item>
///   <item>Update + Update(s) → single <c>Update</c> with the latest payload.</item>
/// </list>
/// Input order is preserved by first-appearance of each entity id. An entity whose ops cancelled out
/// has no op in the result - callers remove its originals without sending anything.
/// </summary>
internal static class OperationCoalescer
{
    public static IReadOnlyList<SyncOperation> Coalesce(IList<SyncOperation> orderedOps)
    {
        // null = the entity's ops so far cancelled each other out
        var byEntity = new Dictionary<string, SyncOperation?>();
        var order = new List<string>();

        // entities whose current run of ops began with a Create that never reached the server
        var neverSent = new HashSet<string>();

        foreach (var op in orderedOps)
        {
            var id = op.EntityIdentifier;
            if (!byEntity.TryGetValue(id, out var existing))
            {
                order.Add(id);
                Start(byEntity, neverSent, op);
            }
            else if (existing == null)
            {
                // a new run after a cancelled-out pair (entity recreated under the same id)
                Start(byEntity, neverSent, op);
            }
            else if (op.Verb == SyncVerb.Delete && neverSent.Contains(id))
            {
                byEntity[id] = null;
                neverSent.Remove(id);
            }
            else
            {
                byEntity[id] = Merge(existing, op);
            }
        }

        return order
            .Select(id => byEntity[id])
            .Where(op => op != null)
            .Select(op => op!)
            .ToList();
    }


    static void Start(Dictionary<string, SyncOperation?> byEntity, HashSet<string> neverSent, SyncOperation op)
    {
        byEntity[op.EntityIdentifier] = op;
        if (IsUnsentCreate(op))
            neverSent.Add(op.EntityIdentifier);
        else
            neverSent.Remove(op.EntityIdentifier);
    }


    /// <summary>
    /// A Create with no attempt recorded and still Pending was never handed to the transport. Once an attempt was
    /// made the server may have it (a timeout can't tell), so it no longer qualifies.
    /// </summary>
    static bool IsUnsentCreate(SyncOperation op)
        => op.Verb == SyncVerb.Create && op.Attempts == 0 && op.State == SyncOperationState.Pending;


    static SyncOperation Merge(SyncOperation existing, SyncOperation next)
    {
        // Trailing Delete wins (a Create that may have reached the server still needs the DELETE).
        if (next.Verb == SyncVerb.Delete)
            return next with { Identifier = existing.Identifier, CreatedAt = existing.CreatedAt };

        // Create + Update → keep Create verb, take latest payload.
        if (existing.Verb == SyncVerb.Create && next.Verb == SyncVerb.Update)
            return existing with { Payload = next.Payload };

        // Update + Update → latest payload wins.
        if (existing.Verb == SyncVerb.Update && next.Verb == SyncVerb.Update)
            return existing with { Payload = next.Payload };

        // Delete + anything new for the same id is an unusual sequence (entity recreated under
        // the same id). Treat as a fresh Create with the new payload.
        if (existing.Verb == SyncVerb.Delete && next.Verb == SyncVerb.Create)
            return next with { Identifier = existing.Identifier, CreatedAt = existing.CreatedAt };

        // Anything else: prefer the latest verb + payload.
        return next with { Identifier = existing.Identifier, CreatedAt = existing.CreatedAt };
    }
}

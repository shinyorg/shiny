using System.Collections.Generic;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Coalesces a sequence of outbox operations for a single endpoint into the minimum
/// equivalent set that produces the same final server-side state. Rules:
/// <list type="bullet">
///   <item>Trailing <c>Delete</c> wins — preceding Create/Update for the same entity drop.</item>
///   <item>Create + Update(s) → single <c>Create</c> with the latest payload.</item>
///   <item>Update + Update(s) → single <c>Update</c> with the latest payload.</item>
/// </list>
/// Input order is preserved by first-appearance of each entity id.
/// </summary>
internal static class OperationCoalescer
{
    public static IReadOnlyList<SyncOperation> Coalesce(IList<SyncOperation> orderedOps)
    {
        var byEntity = new Dictionary<string, SyncOperation>();
        var order = new List<string>();

        foreach (var op in orderedOps)
        {
            if (!byEntity.TryGetValue(op.EntityIdentifier, out var existing))
            {
                byEntity[op.EntityIdentifier] = op;
                order.Add(op.EntityIdentifier);
            }
            else
            {
                var merged = Merge(existing, op);
                byEntity[op.EntityIdentifier] = merged;
            }
        }

        var result = new List<SyncOperation>(order.Count);
        foreach (var id in order)
            result.Add(byEntity[id]);
        return result;
    }


    static SyncOperation Merge(SyncOperation existing, SyncOperation next)
    {
        // Trailing Delete always wins.
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

using System.Collections.Generic;
using System.Text.Json;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Parses the standard batch response shape:
/// <c>{ "results": [{ "id": "...", "status": 200, "body": {...}, "error": null }, ...] }</c>.
/// Missing entries default to the outer status code with no body.
/// </summary>
internal static class BatchResultParser
{
    public static SyncBatchResult Parse(int outerStatusCode, IReadOnlyList<SyncOperation> ops, string? body)
    {
        var byId = new Dictionary<string, SyncBatchItemResult>();

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        if (el.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
                        {
                            var id = idEl.GetString()!;
                            var status = el.TryGetProperty("status", out var s) ? s.GetInt32() : outerStatusCode;
                            var resp = el.TryGetProperty("body", out var b) ? b.GetRawText() : null;
                            var isConflict = status == 409 || status == 412;
                            var err = el.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

                            byId[id] = new SyncBatchItemResult(id, status, resp, isConflict, err);
                        }
                    }
                }
            }
            catch
            {
                // fall through to the default-fill path below
            }
        }

        var items = new List<SyncBatchItemResult>(ops.Count);
        foreach (var op in ops)
        {
            if (byId.TryGetValue(op.Identifier, out var r))
                items.Add(r);
            else
                items.Add(new SyncBatchItemResult(op.Identifier, outerStatusCode, null, false, null));
        }
        return new SyncBatchResult(outerStatusCode, items);
    }
}

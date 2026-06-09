using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shiny.Data.Sync;


/// <summary>
/// Cross-cutting hook invoked on every sync HTTP request the engine emits. Register one
/// or more in DI (<c>services.AddSingleton&lt;ISyncInterceptor, MyInterceptor&gt;()</c>) to
/// attach auth headers, trace ids, or signing once for every endpoint. All registered
/// interceptors run before the per-endpoint <see cref="SyncEndpoint.OnBeforeSend"/> hook,
/// so endpoint-specific logic still wins on header conflicts.
/// </summary>
public interface ISyncInterceptor
{
    /// <summary>Called before each inbox pull request.</summary>
    Task BeforePull(SyncEndpoint endpoint, string? cursor, HttpRequestMessage request);

    /// <summary>
    /// Called before each outbox push. For batched endpoints <paramref name="operations"/>
    /// contains the full coalesced batch; for single-send endpoints it's a one-element list.
    /// </summary>
    Task BeforePush(SyncEndpoint endpoint, IReadOnlyList<SyncOperation> operations, HttpRequestMessage request);

    /// <summary>
    /// Called before each tombstone-fetch request. Default forwards to <see cref="BeforePull"/>
    /// so most implementations only need to fill that one in.
    /// </summary>
    Task BeforeTombstoneFetch(SyncEndpoint endpoint, string? cursor, HttpRequestMessage request)
        => this.BeforePull(endpoint, cursor, request);
}

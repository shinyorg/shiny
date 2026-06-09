using System;
using System.Collections.Generic;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Holds the set of <see cref="SyncEndpoint"/>s registered at host build time.
/// </summary>
public class SyncEndpointRegistry : ISyncEndpointBuilder
{
    readonly Dictionary<string, SyncEndpoint> byKey = new();
    readonly Dictionary<Type, SyncEndpoint> byType = new();

    public void RegisterEndpoint<T>(string url, Action<SyncEndpoint>? configure = null) where T : ISyncEntity
    {
        var endpoint = new SyncEndpoint
        {
            Key = typeof(T).FullName!,
            EntityType = typeof(T),
            Url = url,
            SerializeEntity = static entity => Json.Default.Serialize<T>((T)entity),
            DeserializeEntity = static payload => Json.Default.Deserialize<T>(payload)
        };
        configure?.Invoke(endpoint);

        if (this.byKey.ContainsKey(endpoint.Key))
            throw new InvalidOperationException(
                $"A sync endpoint with key '{endpoint.Key}' is already registered. " +
                "Each entity type may only be registered once per host."
            );

        this.byKey[endpoint.Key] = endpoint;
        this.byType[typeof(T)] = endpoint;
    }

    public SyncEndpoint? Get(string key)
        => this.byKey.TryGetValue(key, out var ep) ? ep : null;

    public SyncEndpoint? Get(Type entityType)
        => this.byType.TryGetValue(entityType, out var ep) ? ep : null;

    public SyncEndpoint? Get<T>() where T : ISyncEntity
        => this.Get(typeof(T));

    public IReadOnlyCollection<SyncEndpoint> All => this.byKey.Values;
}

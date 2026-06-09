using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Data.Sync.Tests.Fakes;


/// <summary>
/// Thread-safe in-memory <see cref="IRepository"/> for unit tests. Keyed by
/// <c>(EntityType, Identifier)</c>; fires <see cref="ActionOccurred"/> on every mutation
/// just like the production repositories the engine wires up.
/// </summary>
public sealed class InMemoryRepository : IRepository
{
    readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IRepositoryEntity>> storage = new();

    public event EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)>? ActionOccurred;


    ConcurrentDictionary<string, IRepositoryEntity> Bucket<T>() where T : IRepositoryEntity
        => this.storage.GetOrAdd(typeof(T), _ => new());


    public bool Exists<T>(string identifier) where T : IRepositoryEntity
        => this.Bucket<T>().ContainsKey(identifier);


    public T? Get<T>(string identifier) where T : IRepositoryEntity
        => this.Bucket<T>().TryGetValue(identifier, out var v) ? (T)v : default;


    public IReadOnlyList<T> GetAll<T>() where T : IRepositoryEntity
        => this.Bucket<T>().Values.Cast<T>().ToList();


    public bool Set<T>(T entity) where T : IRepositoryEntity
    {
        var bucket = this.Bucket<T>();
        var existed = bucket.ContainsKey(entity.Identifier);
        bucket[entity.Identifier] = entity;
        this.ActionOccurred?.Invoke(this, (existed ? RepositoryAction.Update : RepositoryAction.Add, typeof(T), entity));
        return existed;
    }


    public bool Remove<T>(string identifier) where T : IRepositoryEntity
    {
        var bucket = this.Bucket<T>();
        if (!bucket.TryRemove(identifier, out var removed))
            return false;
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Remove, typeof(T), removed));
        return true;
    }


    public void Clear<T>() where T : IRepositoryEntity
    {
        this.Bucket<T>().Clear();
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Clear, typeof(T), null));
    }


    public void Insert<T>(T entity) where T : IRepositoryEntity
    {
        var bucket = this.Bucket<T>();
        if (!bucket.TryAdd(entity.Identifier, entity))
            throw new RepositoryException($"Entity {typeof(T).Name}/{entity.Identifier} already exists.");
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Add, typeof(T), entity));
    }


    public void Update<T>(T entity) where T : IRepositoryEntity
    {
        var bucket = this.Bucket<T>();
        if (!bucket.ContainsKey(entity.Identifier))
            throw new RepositoryException($"Entity {typeof(T).Name}/{entity.Identifier} does not exist.");
        bucket[entity.Identifier] = entity;
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Update, typeof(T), entity));
    }
}

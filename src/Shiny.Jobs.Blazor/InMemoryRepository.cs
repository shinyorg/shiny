using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Shiny.Support.Repositories;

namespace Shiny.Jobs.Blazor;


public class InMemoryRepository : IRepository
{
    readonly ConcurrentDictionary<(Type Type, string Id), IRepositoryEntity> store = new();
    readonly Subject<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> repoSubj = new();


    public bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity
        => this.store.ContainsKey((typeof(TEntity), identifier));


    public TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        if (this.store.TryGetValue((typeof(TEntity), identifier), out var entity))
            return (TEntity)entity;

        return default;
    }


    public IList<TEntity> GetList<TEntity>(Expression<Func<TEntity, bool>>? expression = null) where TEntity : IRepositoryEntity
    {
        var items = this.store
            .Where(x => x.Key.Type == typeof(TEntity))
            .Select(x => (TEntity)x.Value);

        if (expression != null)
            items = items.Where(expression.Compile());

        return items.ToList();
    }


    public bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = (typeof(TEntity), entity.Identifier);
        var exists = this.store.ContainsKey(key);
        this.store[key] = entity;
        this.repoSubj.OnNext((exists ? RepositoryAction.Update : RepositoryAction.Add, typeof(TEntity), entity));
        return exists;
    }


    public void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = (typeof(TEntity), entity.Identifier);
        if (!this.store.TryAdd(key, entity))
            throw new InvalidOperationException($"Entity with identifier '{entity.Identifier}' already exists");

        this.repoSubj.OnNext((RepositoryAction.Add, typeof(TEntity), entity));
    }


    public void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = (typeof(TEntity), entity.Identifier);
        if (!this.store.ContainsKey(key))
            throw new InvalidOperationException($"Entity with identifier '{entity.Identifier}' does not exist");

        this.store[key] = entity;
        this.repoSubj.OnNext((RepositoryAction.Update, typeof(TEntity), entity));
    }


    public bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        var key = (typeof(TEntity), identifier);
        if (this.store.TryRemove(key, out var entity))
        {
            this.repoSubj.OnNext((RepositoryAction.Remove, typeof(TEntity), entity));
            return true;
        }
        return false;
    }


    public void Clear<TEntity>() where TEntity : IRepositoryEntity
    {
        var keys = this.store.Keys.Where(x => x.Type == typeof(TEntity)).ToList();
        foreach (var key in keys)
            this.store.TryRemove(key, out _);

        this.repoSubj.OnNext((RepositoryAction.Clear, typeof(TEntity), null));
    }


    public IObservable<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> WhenActionOccurs()
        => this.repoSubj;
}

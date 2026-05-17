using System;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Shiny.Stores;
using Shiny.Support.Repositories;

namespace Shiny.Storage.Blazor;


public class LocalStorageRepository : IRepository
{
    const string KeyPrefix = "shiny:repo:";

    readonly IJSInProcessRuntime js;
    readonly ISerializer serializer;


    public LocalStorageRepository(IJSRuntime jsRuntime, ISerializer serializer)
    {
        this.js = (IJSInProcessRuntime)jsRuntime;
        this.serializer = serializer;
    }


    public event EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)>? ActionOccurred;


    public bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity
        => this.js.Invoke<bool>("shinyLocalStorage.containsKey", GetKey<TEntity>(identifier));


    public TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        var json = this.js.Invoke<string?>("shinyLocalStorage.getItem", GetKey<TEntity>(identifier));
        if (json == null)
            return default;

        return this.serializer.Deserialize<TEntity>(json);
    }


    public IReadOnlyList<TEntity> GetAll<TEntity>() where TEntity : IRepositoryEntity
    {
        var keys = this.js.Invoke<string[]>("shinyLocalStorage.getKeys", GetTypePrefix<TEntity>());
        var items = new List<TEntity>(keys.Length);

        foreach (var key in keys)
        {
            var json = this.js.Invoke<string?>("shinyLocalStorage.getItem", key);
            if (json != null)
            {
                var entity = this.serializer.Deserialize<TEntity>(json);
                if (entity != null)
                    items.Add(entity);
            }
        }

        return items;
    }


    public bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(entity.Identifier);
        var exists = this.js.Invoke<bool>("shinyLocalStorage.containsKey", key);
        this.js.InvokeVoid("shinyLocalStorage.setItem", key, this.serializer.Serialize(entity));

        var action = exists ? RepositoryAction.Update : RepositoryAction.Add;
        this.ActionOccurred?.Invoke(this,(action, typeof(TEntity), entity));
        return exists;
    }


    public void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(entity.Identifier);
        if (this.js.Invoke<bool>("shinyLocalStorage.containsKey", key))
            throw new RepositoryException($"{typeof(TEntity).FullName} already has a record with identifier '{entity.Identifier}'");

        this.js.InvokeVoid("shinyLocalStorage.setItem", key, this.serializer.Serialize(entity));
        this.ActionOccurred?.Invoke(this,(RepositoryAction.Add, typeof(TEntity), entity));
    }


    public void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(entity.Identifier);
        if (!this.js.Invoke<bool>("shinyLocalStorage.containsKey", key))
            throw new RepositoryException($"{typeof(TEntity).FullName} - no record exists with identifier '{entity.Identifier}'");

        this.js.InvokeVoid("shinyLocalStorage.setItem", key, this.serializer.Serialize(entity));
        this.ActionOccurred?.Invoke(this,(RepositoryAction.Update, typeof(TEntity), entity));
    }


    public bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(identifier);
        var entity = this.Get<TEntity>(identifier);
        var removed = this.js.Invoke<bool>("shinyLocalStorage.removeItem", key);

        if (removed && entity != null)
            this.ActionOccurred?.Invoke(this,(RepositoryAction.Remove, typeof(TEntity), entity));

        return removed;
    }


    public void Clear<TEntity>() where TEntity : IRepositoryEntity
    {
        this.js.Invoke<int>("shinyLocalStorage.removeKeys", GetTypePrefix<TEntity>());
        this.ActionOccurred?.Invoke(this,(RepositoryAction.Clear, typeof(TEntity), default));
    }


    static string GetTypePrefix<TEntity>() => $"{KeyPrefix}{typeof(TEntity).Name}:";
    static string GetKey<TEntity>(string identifier) => $"{GetTypePrefix<TEntity>()}{identifier}";
}

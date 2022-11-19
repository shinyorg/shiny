using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Stores.Impl;


public class LocalStorageRepository<TStoreConverter, TEntity> : IRepository<TEntity>
    where TStoreConverter : class, IStoreConverter<TEntity>, new()
    where TEntity : IStoreEntity
{
    readonly Subject<(RepositoryAction Action, TEntity? Entity)> repoSubj = new();
    readonly TStoreConverter converter = new();

    //readonly IJSInProcessRuntime jsRuntime;
    readonly ISerializer serializer;


    //public LocalStorageRepository(IJSRuntime jsRuntime, ISerializer serializer)
    //{
    //    this.jsRuntime = (IJSInProcessRuntime)jsRuntime;
    //    this.serializer = serializer;
    //}


    //IJSInProcessObjectReference? jsRef;
    //async Task<IJSInProcessObjectReference> Mod()
    //{
    //    this.jsRef ??= await this.jsRuntime.ImportInProcess("Shiny.Core.Blazor", "storage.js");
    //    return this.jsRef;
    //}


    public async Task<bool> Exists(string key)
    {
        var fk = GetFullKey(key);
        //return (await this.Mod()).Invoke<bool>("exists", fk);
        return false;
    }


    public async Task<TEntity?> Get(string key)
    {
        var fk = GetFullKey(key);
        //var json = (await this.Mod()).Invoke<string>("get", fk);

        //if (json == null)
        //    return default;

        //var obj = this.serializer.Deserialize<TEntity>(json);
        //return obj;
        return default;
    }


    public async Task<IList<TEntity>> GetList(Expression<Func<TEntity, bool>>? expression = null)
    {
        var list = new List<TEntity>();
        //var dict = (await this.Mod()).Invoke<Dictionary<string, string>>("getList");
        var dict = new Dictionary<string, string>();

        if (dict.Count > 0)
        {
            var e = expression?.Compile() ?? new Func<TEntity, bool>(_ => true);

            foreach (var pair in dict)
            {
                var obj = this.FromJson(pair.Value);
                if (e(obj))
                    list.Add(obj);
            }
        }
        return list;
    }


    public async Task<bool> Set(TEntity entity)
    {
        if (entity.Identifier.IsEmpty())
            throw new InvalidOperationException("Identifier cannot be empty/null");

        var action = this.Get(entity.Identifier) == null
            ? RepositoryAction.Update
            : RepositoryAction.Add;

        var fk = GetFullKey(entity.Identifier);
        var json = this.ToJson(entity);
        //(await this.Mod()).InvokeVoid("set", fk, json);

        this.repoSubj.OnNext((action, entity));

        return action == RepositoryAction.Add;
    }


    public Task<bool> Remove(string key)
    {
        var fk = GetFullKey(key);
        var exists = Get(key) != null;

        //this.jsRuntime.InvokeVoid("localStorage.removeItem", fk);
        return Task.FromResult(exists);
    }


    public Task Clear()
    {
        //this.jsRuntime.InvokeVoid("localStorage.clear");
        this.repoSubj.OnNext((RepositoryAction.Clear, default));
        return Task.CompletedTask;
    }


    public IObservable<(RepositoryAction Action, TEntity? Entity)> WhenActionOccurs() => this.repoSubj;


    TEntity? DoGet(string key)
    {
        var fk = GetFullKey(key);
        //var json = this.jsRuntime.Invoke<string>(fk);
        //if (json == null)
        //    return default;

        //var obj = this.FromJson(json);
        //return obj;
        return default;
    }


    string ToJson(TEntity entity)
    {
        var store = this.converter.ToStore(entity);
        var json = this.serializer.Serialize(store);
        return json;
    }


    TEntity FromJson(string json)
    {
        var dict = this.serializer.Deserialize<Dictionary<string, object>>(json);
        var obj = this.converter.FromStore(dict);
        return obj;
    }


    static string GetFullKey(string key) => $"s_{typeof(TEntity).FullName}_{key}";
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.Stores.Impl;


public class LocalStorageRepository<TStoreConverter, TEntity> : IRepository<TEntity>
    where TStoreConverter : class, IStoreConverter<TEntity>, new()
    where TEntity : IStoreEntity
{
    readonly Subject<(RepositoryAction Action, TEntity? Entity)> repoSubj = new();
    readonly TStoreConverter converter = new();

    readonly IJSInProcessRuntime jsRuntime;
    readonly ISerializer serializer;


    public LocalStorageRepository(IJSRuntime jsRuntime, ISerializer serializer)
    {
        this.jsRuntime = (IJSInProcessRuntime)jsRuntime;
        this.serializer = serializer;
    }


    public async Task<bool> Exists(string key)
    {
        //var path = this.GetPath(key);
        //var exists = File.Exists(path);
        //return Task.FromResult(exists);
        return false;
    }


    public async Task<TEntity?> Get(string key)
    {
        var value = this.jsRuntime.Invoke<string>("localStorage.getItem", key);
        if (value == null)
            return default;

        this.serializer.Deserialize<TEntity>(value);
        //TEntity? result = default;
        //await this.InTransaction(list =>
        //{
        //    if (list.ContainsKey(key))
        //        result = list[key];
        //});
        //return result;
        return default;
    }


    public async Task<IList<TEntity>> GetList(Expression<Func<TEntity, bool>>? expression = null)
    {
        //var result = new List<TEntity>();
        //await this
        //    .InTransaction(
        //        list => result.AddRange(list
        //            .Values
        //            .WhereIf(expression)
        //        )
        //    )
        //    .ConfigureAwait(false);
        //return result;
        return null;
    }


    public async Task<bool> Set(TEntity entity)
    {
        //var update = true;
        //await this.InTransaction(list =>
        //{
        //    update = this.Write(entity);
        //    list[entity.Identifier] = entity;

        //    var action = update ? RepositoryAction.Update : RepositoryAction.Add;
        //    this.repoSubj.OnNext((action, entity));
        //});
        //return update;
        return true;
    }


    public async Task<bool> Remove(string key)
    {
        // TODO: run get first
        // TODO: to full key
        this.jsRuntime.InvokeVoid("localStorage.removeItem", key);
        return false;
    }


    public Task Clear()
    {
        this.jsRuntime.InvokeVoid("localStorage.clear");
        this.repoSubj.OnNext((RepositoryAction.Clear, default));
        return Task.CompletedTask;
    }


    public IObservable<(RepositoryAction Action, TEntity? Entity)> WhenActionOccurs() => this.repoSubj;


    //bool Write(TEntity entity)
    //{
    //    var path = this.GetPath(entity.Identifier);
    //    var update = File.Exists(path);

    //    var serialize = this.converter.ToStore(entity).ToDictionary(
    //        x => x.Property,
    //        x => x.Value
    //    );
    //    if (!serialize.ContainsKey(nameof(IStoreEntity.Identifier)))
    //        serialize.Add(nameof(IStoreEntity.Identifier), entity.Identifier);

    //    var value = this.serializer.Serialize(serialize);
    //    File.WriteAllText(path, value);
    //    return update;
    //}


    //Dictionary<string, TEntity> Load()
    //{
    //        var dictionary = this.serializer.Deserialize<Dictionary<string, object>>(text);
    //        foreach (var pair in dictionary)
    //        {
    //            var el = (JsonElement)pair.Value;
    //            switch (el.ValueKind)
    //            {
    //                case JsonValueKind.String:
    //                    dictionary[pair.Key] = el.GetString();
    //                    break;

    //                case JsonValueKind.Number:
    //                    if (el.TryGetInt64(out var longValue))
    //                    {
    //                        dictionary[pair.Key] = longValue;
    //                    }
    //                    else
    //                    {
    //                        dictionary[pair.Key] = el.GetDouble();
    //                    }
    //                    break;

    //                case JsonValueKind.True:
    //                case JsonValueKind.False:
    //                    dictionary[pair.Key] = el.GetBoolean();
    //                    break;

    //                default:
    //                    throw new ArgumentException("Invalid ValueKind - " + el.ValueKind);
    //            }
    //        }

    //        var entity = this.converter.FromStore(dictionary);
    //        if (entity.Identifier.IsEmpty())
    //            throw new InvalidOperationException("Identifier not set on store entity");

    //        dict.Add(entity.Identifier, entity);

}
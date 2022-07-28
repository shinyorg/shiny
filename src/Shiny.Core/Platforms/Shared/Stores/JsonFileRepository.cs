using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Stores.Impl;


public class JsonFileRepository<TStoreConverter, TEntity> : IRepository<TEntity>
    where TStoreConverter : class, IStoreConverter<TEntity>, new()
    where TEntity : IStoreEntity
{
    readonly TStoreConverter converter = new();
    readonly IPlatform platform;
    readonly ISerializer serializer;


    public JsonFileRepository(IPlatform platform, ISerializer serializer)
    {
        this.platform = platform;
        this.serializer = serializer;
    }


    public Task<bool> Exists(string key)
    {
        var path = this.GetPath(key);
        var exists = File.Exists(path);
        return Task.FromResult(exists);
    }


    public async Task<TEntity> Get(string key)
    {
        TEntity result = default;
        await this.InTransaction(list =>
        {
            if (list.ContainsKey(key))
                result = list[key];
        });
        return result;
    }


    public async Task<IList<TEntity>> GetList(Expression<Func<TEntity, bool>>? expression = null)
    {
        var result = new List<TEntity>();
        await this
            .InTransaction(
                list => result.AddRange(list
                    .Values
                    .WhereIf(expression)
                )
            )
            .ConfigureAwait(false);
        return result;
    }


    public async Task<bool> Set(TEntity entity)
    {
        var update = true;
        await this.InTransaction(list =>
        {
            update = this.Write(entity);
            list[entity.Identifier] = entity;
        });
        return update;
    }


    public Task<bool> Remove(string key)
    {
        var tcs = new TaskCompletionSource<bool>();
        this.InTransaction(list =>
        {
            var path = this.GetPath(key);

            if (!File.Exists(path))
            {
                tcs.TrySetResult(false);
            }
            else
            {
                var entity = list[key];
                list.Remove(key);
                File.Delete(path);
                tcs.TrySetResult(true);
            }
        });
        return tcs.Task;
    }


    public Task Clear() => this.InTransaction(list =>
    {
        if (!list.Any())
            return;

        list.Clear();
        var files = this.GetFiles();
        foreach (var file in files)
            file.Delete();
    });


    FileInfo[] GetFiles() => this.platform.AppData.GetFiles($"{typeof(TEntity).Name}_*.shiny");


    string GetPath(string key)
    {
        var fileName = $"{typeof(TEntity).Name}_{key}.shiny";
        var path = Path.Combine(this.platform.AppData.FullName, fileName);
        return path;
    }


    bool Write(TEntity entity)
    {
        var path = this.GetPath(entity.Identifier);
        var update = File.Exists(path);

        var serialize = this.converter.ToStore(entity).ToDictionary(
            x => x.Property,
            x => x.Value
        );
        if (!serialize.ContainsKey(nameof(IStoreEntity.Identifier)))
            serialize.Add(nameof(IStoreEntity.Identifier), entity.Identifier);

        var value = this.serializer.Serialize(serialize);
        File.WriteAllText(path, value);
        return update;
    }


    readonly object syncLock = new();
    Dictionary<string, TEntity>? memory;
    Task InTransaction(Action<Dictionary<string, TEntity>> action) => Task.Run(() =>
    {
        lock (this.syncLock)
        {
            this.memory ??= this.Load();
            action(this.memory);
        }
    });


    Dictionary<string, TEntity> Load()
    {
        var dict = new Dictionary<string, TEntity>();
        var files = this.GetFiles();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file.FullName);
            var dictionary = this.serializer.Deserialize<Dictionary<string, object>>(text);
            foreach (var pair in dictionary)
            {
                var el = (JsonElement)pair.Value;
                switch (el.ValueKind)
                {
                    case JsonValueKind.String:
                        dictionary[pair.Key] = el.GetString();
                        break;

                    case JsonValueKind.Number:
                        if (el.TryGetInt64(out var longValue))
                        {
                            dictionary[pair.Key] = longValue;
                        }
                        else
                        {
                            dictionary[pair.Key] = el.GetDouble();
                        }
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        dictionary[pair.Key] = el.GetBoolean();
                        break;

                    default:
                        throw new ArgumentException("Invalid ValueKind - " + el.ValueKind);
                }
            }

            var entity = this.converter.FromStore(dictionary);
            if (entity.Identifier.IsEmpty())
                throw new InvalidOperationException("Identifier not set on store entity");

            dict.Add(entity.Identifier, entity);
        }
        return dict;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Shiny.Stores.Infrastructure;


public class JsonFileRepository : IRepository
{
    readonly Dictionary<Type, Dictionary<string, object>> memory = new();
    readonly IPlatform platform;
    readonly ISerializer serializer;


    public JsonFileRepository(IPlatform platform, ISerializer serializer)
    {
        this.platform = platform;
        this.serializer = serializer;
    }


    public Task<bool> Exists<T>(string key) where T : class
    {
        var path = this.GetPath(typeof(T), key);
        var exists = File.Exists(path);
        return Task.FromResult(exists);
    }


    public async Task<T?> Get<T>(string key) where T : class
    {
        T? result = null;
        await this.InTransaction(typeof(T), list =>
        {
            if (list.ContainsKey(key))
                result = (T)list[key];
        });
        return result;
    }


    public async Task<IDictionary<string, T>> GetListWithKeys<T>(Expression<Func<T, bool>>? expression = null) where T : class
    {
        var filter = expression?.Compile() ?? new Func<T, bool>(_ => true);
        var result = new Dictionary<string, T>();

        await this.InTransaction(typeof(T), list =>
        {
            foreach (var pair in list)
            {
                var value = (T)pair.Value;
                if (filter(value))
                    result.Add(pair.Key, (T)pair.Value);
            }
        });
        return result;
    }


    public async Task<IList<T>> GetList<T>(Expression<Func<T, bool>>? expression = null) where T : class
    {
        var result = new List<T>();
        await this.InTransaction(
            typeof(T),
            list => result
                .AddRange(list
                    .Values
                    .OfType<T>()
                    .WhereIf(expression)
                )
        );
        return result;
    }


    public async Task<bool> Set(string key, object entity)
    {
        var update = true;
        await this.InTransaction(entity.GetType(), list =>
        {
            update = this.Write(key, entity);
            list[key] = entity;
        });
        return update;
    }


    public Task<bool> Remove<T>(string key) where T : class
    {
        var tcs = new TaskCompletionSource<bool>();
        this.InTransaction(typeof(T), list =>
        {
            var path = this.GetPath(typeof(T), key);

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


    public Task Clear<T>() where T : class => this.InTransaction(typeof(T), list =>
    {
        if (!list.Any())
            return;

        list.Clear();
        var files = this.GetTypeFiles(typeof(T));
        foreach (var file in files)
            File.Delete(file.FullName);
    });


    FileInfo[] GetTypeFiles(Type type) => this.platform.AppData.GetFiles($"{type.Name}_*.core");


    string GetPath(Type type, string key)
    {
        var fileName = $"{type.Name}_{key}.core";
        var path = Path.Combine(this.platform.AppData.FullName, fileName);
        return path;
    }


    bool Write(string key, object entity)
    {
        var path = this.GetPath(entity.GetType(), key);
        var update = File.Exists(path);
        var value = this.serializer.Serialize(entity);
        File.WriteAllText(path, value);
        return update;
    }


    Task InTransaction(Type type, Action<Dictionary<string, object>> action) => Task.Run(() =>
    {
        lock (this.memory)
        {
            if (!this.memory.ContainsKey(type))
            {
                var dict = new Dictionary<string, object>();
                var files = this.GetTypeFiles(type);

                foreach (var file in files)
                {
                    var text = File.ReadAllText(file.FullName);
                    var obj = this.serializer.Deserialize(type, text);
                    var key = file
                        .Name
                        .Replace($"{type.Name}_", String.Empty)
                        .Replace(".core", String.Empty);
                    dict.Add(key, obj);
                }
                this.memory.Add(type, dict);
            }

            action(this.memory[type]);
        }
    });
}
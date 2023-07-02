#if PLATFORM
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Support.Repositories.Impl;


public class FileSystemRepository : IRepository
{    
    readonly ISerializer serializer;
    readonly DirectoryInfo rootDir;
    readonly ILogger logger;


    public FileSystemRepository(
        IPlatform platform,
        ISerializer serializer,
        ILogger<FileSystemRepository> logger
    )
    {
        this.serializer = serializer;
        this.rootDir = platform.AppData;
        this.logger = logger;
    }


    public bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        var path = this.GetPath<TEntity>(identifier);
        var exists = File.Exists(path);
        return exists;
    }


    public TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        TEntity? result = default;
        this.InTransaction<TEntity>(list =>
        {
            if (list.ContainsKey(identifier))
                result = list[identifier];
        });
        return result;
    }


    public IList<TEntity> GetList<TEntity>(Expression<Func<TEntity, bool>>? expression = null) where TEntity : IRepositoryEntity
    {
        var result = new List<TEntity>();
        this.InTransaction<TEntity>(list => result.AddRange(list
            .Values
            .WhereIf(expression)
        ));
        return result;
    }


    public void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        this.InTransaction<TEntity>(list =>
        {
            if (list.ContainsKey(entity.Identifier))
                throw new ArgumentException($"{typeof(TEntity).FullName} already has a record with identifier '{entity.Identifier}'");

            this.Write(entity);
            list[entity.Identifier] = entity;
            this.repoSubj.OnNext((RepositoryAction.Add, typeof(TEntity), entity));
        });
    }


    public void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        this.InTransaction<TEntity>(list =>
        {
            if (!list.ContainsKey(entity.Identifier))
                throw new ArgumentException($"{typeof(TEntity).FullName} already has a record with identifier '{entity.Identifier}'");

            this.Write(entity);
            list[entity.Identifier] = entity;
            this.repoSubj.OnNext((RepositoryAction.Update, typeof(TEntity), entity));
        });
    }


    public bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var update = true;
        this.InTransaction<TEntity>(list =>
        {
            update = this.Write<TEntity>(entity);
            list[entity.Identifier] = entity;

            var action = update ? RepositoryAction.Update : RepositoryAction.Add;
            this.repoSubj.OnNext((action, typeof(TEntity), entity));
        });
        return update;
    }


    public bool Remove<TEntity>(string key) where TEntity : IRepositoryEntity
    {
        var removed = false;
        this.InTransaction<TEntity>(list =>
        {
            var path = this.GetPath<TEntity>(key);

            if (File.Exists(path))
            {
                var entity = list[key];
                list.Remove(key);
                File.Delete(path);
                removed = true;

                this.repoSubj.OnNext((RepositoryAction.Remove, typeof(TEntity), (object)entity));
            }
        });
        return removed;
    }


    public void Clear<TEntity>() where TEntity : IRepositoryEntity => this.InTransaction<TEntity>(list =>
    {
        if (!list.Any())
            return;

        list.Clear();
        var files = this.GetFiles<TEntity>();
        foreach (var file in files)
            file.Delete();

        this.repoSubj.OnNext((RepositoryAction.Clear, typeof(TEntity), default));
    });


    readonly Subject<(RepositoryAction Action, Type EntityType, object? Entity)> repoSubj = new();
    public IObservable<(RepositoryAction Action, Type EntityType, object? Entity)> WhenActionOccurs() => this.repoSubj;


    FileInfo[] GetFiles<TEntity>() => this.rootDir.GetFiles($"{typeof(TEntity).Name}_*.shiny");


    string GetPath<TEntity>(string identifier)
    {
        var fileName = $"{typeof(TEntity).Name}_{identifier}.shiny";
        var path = Path.Combine(this.rootDir.FullName, fileName);
        return path;
    }


    bool Write<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var path = this.GetPath<TEntity>(entity.Identifier);
        var update = File.Exists(path);
        var value = this.serializer.Serialize(entity!);

        File.WriteAllText(path, value);
        return update;
    }


    readonly object syncLock = new();
    readonly Dictionary<string, Dictionary<string, object>> memory = new();
    void InTransaction<TEntity>(Action<Dictionary<string, TEntity>> action) where TEntity : IRepositoryEntity
    {
        var en = typeof(TEntity).Name;

        lock (this.syncLock)
        {
            if (this.memory.ContainsKey(en))
            {
                var entityDictionary = this.memory[en].ToDictionary(
                    x => x.Key,
                    x => (TEntity)x.Value,
                    StringComparer.InvariantCultureIgnoreCase
                );
                action(entityDictionary);

                // write back
                this.memory[en] = entityDictionary.ToDictionary(
                    x => x.Key,
                    x => (object)x.Value,
                    StringComparer.InvariantCultureIgnoreCase
                );
            }
            else
            {
                var entityDictionary = this.Load<TEntity>();
                action(entityDictionary);

                this.memory.Add(en, entityDictionary.ToDictionary(
                    x => x.Key,
                    x => (object)x.Value,
                    StringComparer.InvariantCultureIgnoreCase
                ));
            }
        }
    }


    Dictionary<string, TEntity> Load<TEntity>() where TEntity : IRepositoryEntity
    {
        var dict = new Dictionary<string, TEntity>();
        var files = this.GetFiles<TEntity>();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file.FullName);
            var entity = this.serializer.Deserialize<TEntity>(text);
            dict.Add(entity.Identifier, entity);
        }
        return dict;
    }
}
#endif
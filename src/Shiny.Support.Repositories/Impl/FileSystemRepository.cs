#if PLATFORM
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Support.Repositories.Impl;


public class FileSystemRepository : IRepository
{    
    readonly ISerializer serializer;
    readonly ILogger logger;
    readonly DirectoryInfo rootDir;
    readonly ShinySubject<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> repoSubj;
    //readonly Subject<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> repoSubj = new();


    public FileSystemRepository(
        IPlatform platform,
        ISerializer serializer,
        ILogger<FileSystemRepository> logger
    )
    {
        //Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) <= replace platform
        this.serializer = serializer;
        this.rootDir = platform.AppData;
        this.logger = logger;

        this.repoSubj = new(this.logger);
    }


    public IObservable<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> WhenActionOccurs() => this.repoSubj;

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
            {
                result = list[identifier];
            }
            else
            {
                this.logger.GetNotExists(typeof(TEntity).Name, identifier, list.Count);
            }
        });
        return result;
    }


    public IList<TEntity> GetList<TEntity>(Expression<Func<TEntity, bool>>? expression = null) where TEntity : IRepositoryEntity
    {
        var result = new List<TEntity>();
        this.InTransaction<TEntity>(list =>
            result.AddRange(list
                .Values
                .WhereIf(expression)
            )
        );
        return result;
    }


    public void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        this.InTransaction<TEntity>(list =>
        {
            if (list.ContainsKey(entity.Identifier))
                throw new RepositoryException($"{typeof(TEntity).FullName} already has a record with identifier '{entity.Identifier}'");

            this.Write(entity);
            list.Add(entity.Identifier, entity);
            this.logger.Change(RepositoryAction.Add, typeof(TEntity).Name, entity.Identifier);
        });
        this.repoSubj.OnNext((RepositoryAction.Add, typeof(TEntity), entity));
    }


    public void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        this.InTransaction<TEntity>(list =>
        {
            if (!list.ContainsKey(entity.Identifier))
                throw new RepositoryException($"{typeof(TEntity).FullName} - no record exists with identifier '{entity.Identifier}'");

            this.Write(entity);
            list[entity.Identifier] = entity;
            this.logger.Change(RepositoryAction.Update, typeof(TEntity).Name, entity.Identifier);
        });
        this.repoSubj.OnNext((RepositoryAction.Update, typeof(TEntity), entity));
    }


    public bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var update = true;
        var action = RepositoryAction.Update;

        this.InTransaction<TEntity>(list =>
        {
            update = this.Write(entity);
            list[entity.Identifier] = entity;

            action = update ? RepositoryAction.Update : RepositoryAction.Add;
            this.logger.Change(action, typeof(TEntity).Name, entity.Identifier);
        });
        this.repoSubj.OnNext((action, typeof(TEntity), entity));
        return update;
    }


    public bool Remove<TEntity>(string key) where TEntity : IRepositoryEntity
    {
        var removed = false;
        IRepositoryEntity? entity = null;

        this.InTransaction<TEntity>(list =>
        {
            var path = this.GetPath<TEntity>(key);
            if (list.ContainsKey(key))
            {
                entity = list[key];
                removed = list.Remove(key);
            }
            if (File.Exists(path))
                File.Delete(path);

            this.logger.Change(RepositoryAction.Remove, typeof(TEntity).Name, key);
        });

        if (removed && entity != null)
            this.repoSubj.OnNext((RepositoryAction.Remove, typeof(TEntity), entity));

        return removed;
    }


    public void Clear<TEntity>() where TEntity : IRepositoryEntity
    {
        this.InTransaction<TEntity>(list =>
        {
            var write = list.Count > 0;

            list.Clear();
            var files = this.GetFiles<TEntity>();
            foreach (var file in files)
                file.Delete();

            this.logger.Change(RepositoryAction.Clear, typeof(TEntity).Name, String.Empty);
        });
        this.repoSubj.OnNext((RepositoryAction.Clear, typeof(TEntity), default));
    }


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
    readonly Dictionary<string, object> memory = new();
    void InTransaction<TEntity>(Action<Dictionary<string, TEntity>> action) where TEntity : IRepositoryEntity
    {
        var en = typeof(TEntity).Name; // TODO: this may NOT be enough going forward

        lock (this.syncLock)
        {
            if (this.memory.ContainsKey(en))
            {
                var entityDictionary = (Dictionary<string, TEntity>)this.memory[en];
                this.logger.InternalCount("Read From Cache", en, entityDictionary.Count);
                action(entityDictionary);
                this.logger.InternalCount("Post Cache Operation", en, entityDictionary.Count);
            }
            else
            {
                var entityDictionary = this.Load<TEntity>();
                this.logger.InternalCount("Initial Cache Load", en, entityDictionary.Count);
                action(entityDictionary);
                this.memory.Add(en, entityDictionary);

                this.logger.InternalCount("Post Cache Load Write", en, entityDictionary.Count);
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
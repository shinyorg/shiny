using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.IO;


namespace Shiny.Infrastructure
{
    public class FileSystemRepositoryImpl : IRepository
    {
        readonly IDictionary<Type, Dictionary<string, object>> memory;
        readonly Subject<RepositoryEvent> eventSubject;
        readonly IFileSystem fileSystem;
        readonly ISerializer serializer;


        public FileSystemRepositoryImpl(IFileSystem fileSystem, ISerializer serializer)
        {
            this.fileSystem = fileSystem;
            this.serializer = serializer;
            this.eventSubject = new Subject<RepositoryEvent>();
            this.memory = new Dictionary<Type, Dictionary<string, object>>();
        }


        public IObservable<RepositoryEvent> WhenEvent() => this.eventSubject;


        public async Task<T> Get<T>(string key) where T : class
        {
            var tcs = new TaskCompletionSource<T>();
            await this.InTransaction(typeof(T), list =>
            {
                if (list.ContainsKey(key))
                    tcs.SetResult((T)list[key]);
                else
                    tcs.SetResult(null);
            });

            return await tcs.Task;
        }


        public async Task<List<T>> GetAll<T>() where T : class
        {
            var tcs = new TaskCompletionSource<List<T>>();
            await this.InTransaction(typeof(T), list => tcs.SetResult(list.Values.Cast<T>().ToList()));
            return await tcs.Task;
        }


        public Task Set(string key, object entity) => this.InTransaction(entity.GetType(), list =>
        {
            this.Write(key, entity);
            list[key] = entity;
            this.eventSubject.OnNext(new RepositoryEvent(RepositoryEventType.Add, key, entity));
        });


        public Task<bool> Remove<T>(string key) where T : class
        {
            var tcs = new TaskCompletionSource<bool>();
            this.InTransaction(typeof(T), list =>
            {
                list.Remove(key);
                var path = this.GetPath(typeof(T), key);
                if (!File.Exists(path))
                    tcs.SetResult(false);
                else
                {
                    var entity = this.Get<T>(key);
                    File.Delete(path);
                    this.eventSubject.OnNext(new RepositoryEvent(RepositoryEventType.Remove, key, entity));
                    tcs.SetResult(true);
                }
            });
            return tcs.Task;
        }


        public Task Clear<T>() where T : class => this.InTransaction(typeof(T), list =>
        {
            list.Clear();
            var files = this.GetTypeFiles(typeof(T));
            foreach (var file in files)
                File.Delete(file.FullName);

            this.eventSubject.OnNext(new RepositoryEvent(RepositoryEventType.Clear));
        });


        FileInfo[] GetTypeFiles(Type type) => this.fileSystem.AppData.GetFiles($"{type.Name}_*.core");


        string GetPath(Type type, string key)
        {
            var fileName = $"{type.Name}_{key}.core";
            var path = Path.Combine(this.fileSystem.AppData.FullName, fileName);
            return path;
        }


        void Write(string key, object entity)
        {
            var path = this.GetPath(entity.GetType(), key);
            var value = this.serializer.Serialize(entity);
            File.WriteAllText(path, value);
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
}

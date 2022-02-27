using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Testing.Infrastucture
{
    public class InMemoryRepository : IRepository
    {
        readonly Dictionary<Type, Dictionary<string, object>> data = new Dictionary<Type, Dictionary<string, object>>();


        public Task Clear<T>() where T : class
        {
            this.data.Remove(typeof(T));
            return Task.CompletedTask;
        }


        public Task<bool> Exists<T>(string key) where T : class
        { 
            var result = false;
            if (this.data.ContainsKey(typeof(T)))
                result = this.data[typeof(T)].ContainsKey(key);

            return Task.FromResult(result);
        }


        public Task<T?> Get<T>(string key) where T : class
        {
            var result = default(T);

            if (this.data.ContainsKey(typeof(T)))
            { 
                var dict = this.data[typeof(T)];
                if (dict.ContainsKey(key))
                    result = (T)dict[key];
            }
            return Task.FromResult(result);
        }


        public Task<IList<T>> GetList<T>(Expression<Func<T, bool>>? expression = null) where T : class
        {
            IList<T> result = this.data[typeof(T)]?.Values.OfType<T>().WhereIf(expression).ToList() ?? new List<T>();
            return Task.FromResult(result);
        }


        public Task<IDictionary<string, T>> GetListWithKeys<T>(Expression<Func<T, bool>>? expression = null) where T : class
        {
            var filter = expression?.Compile() ?? new Func<T, bool>(_ => true);

            IDictionary<string, T> result = this.data[typeof(T)]?.Where(x => filter((T)x.Value)).ToDictionary(
                x => x.Key,
                x => (T)x.Value
            ) ?? new Dictionary<string, T>();

            return Task.FromResult(result);
        }


        public Task<bool> Remove<T>(string key) where T : class
        {
            var result = this.data[typeof(T)]?.Remove(key) ?? false;
            return Task.FromResult(result);
        }


        public Task<bool> Set(string key, object entity)
        {
            var type = entity.GetType();
            var result = true;

            if (!this.data.ContainsKey(type))
                this.data[type] = new Dictionary<string, object>();

            var dict = this.data[type];
            if (!dict.ContainsKey(key))
                result = false;

            dict[key] = entity;
            return Task.FromResult(result);
        }
    }
}

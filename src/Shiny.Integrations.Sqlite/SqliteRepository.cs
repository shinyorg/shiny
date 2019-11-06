﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Models;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteRepository : IRepository
    {
        readonly ShinySqliteConnection conn;
        readonly ISerializer serializer;


        public SqliteRepository(ShinySqliteConnection conn, ISerializer serializer)
        {
            this.conn = conn;
            this.serializer = serializer;
        }


        public Task Clear<T>() where T : class
            => this.conn.ExecuteAsync($"DELETE FROM {nameof(RepoStore)} WHERE TypeName = ?", typeof(T).AssemblyQualifiedName);


        public async Task<bool> Exists<T>(string key) where T : class
        {
            var count = await this.conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM {nameof(RepoStore)} WHERE TypeName = ? AND Key = ?",
                typeof(T).AssemblyQualifiedName,
                key
            );
            return count > 0;
        }


        public async Task<T> Get<T>(string key) where T : class
        {
            var item = await this.conn.RepoItems.FirstOrDefaultAsync(x =>
                x.Key == key &&
                x.TypeName == typeof(T).AssemblyQualifiedName
            );
            if (item == null)
                return null;

            var result = this.serializer.Deserialize<T>(item.Blob);
            return result;
        }


        public async Task<IList<T>> GetAll<T>() where T : class
        {
            var result = await this.GetAllWithKeys<T>();
            return new List<T>(result.Values);
        }


        public async Task<IDictionary<string, T>> GetAllWithKeys<T>() where T : class
        {
            var dict = new Dictionary<string, T>();
            var items = await this.conn
                .RepoItems
                .Where(x => x.TypeName == typeof(T).AssemblyQualifiedName)
                .ToListAsync();

            foreach (var item in items)
            {
                var obj = this.serializer.Deserialize<T>(item.Blob);
                dict.Add(item.Key, obj);
            }
            return dict;
        }

        public async Task<bool> Remove<T>(string key) where T : class
        {
            var count = await this.conn.DeleteAsync<RepoStore>(key);
            return (count > 0);
        }


        public async Task<bool> Set(string key, object entity)
        {
            var item = await this.conn.GetAsync<RepoStore>(key);
            if (item == null)
            {
                await this.conn.InsertAsync(new RepoStore
                {
                    Key = key,
                    TypeName = entity.GetType().AssemblyQualifiedName,
                    Blob = this.serializer.Serialize(entity)
                });
                return true;
            }
            else
            {
                item.TypeName = entity.GetType().AssemblyQualifiedName;
                item.Blob = this.serializer.Serialize(entity);
                await this.conn.UpdateAsync(item);
                return false;
            }
        }
    }
}

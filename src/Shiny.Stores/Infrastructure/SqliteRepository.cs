//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.IO;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;

//using Microsoft.Data.Sqlite;

//namespace Shiny.Stores.Infrastructure;

//public interface IEntity
//{
//    string Id { get; set; }
//}

//public class SqliteRepository : IRepository
//{
//    readonly string connectionString;
//    readonly IEnumerable<ITypeConverter> typeConverters;


//    public SqliteRepository(IPlatform platform, IEnumerable<ITypeConverter> typeConverters)
//    {
//        this.connectionString = "Data Source=" + Path.Combine(platform.AppData.FullName, "shiny.db");
//        this.typeConverters = typeConverters;
//    }


//    public Task Clear<T>() where T : class => this.ExecuteNonQueryAsync($"DELETE FROM {typeof(T).Name}");
//    public Task<bool> Exists<T>(string key) where T : class => throw new NotImplementedException();
//    public Task<T?> Get<T>(string key) where T : class => throw new NotImplementedException();
//    public Task<IList<T>> GetList<T>(Expression<Func<T, bool>>? expression = null) where T : class => throw new NotImplementedException();
//    public Task<IDictionary<string, T>> GetListWithKeys<T>(Expression<Func<T, bool>>? expression = null) where T : class => throw new NotImplementedException();
//    public async Task<bool> Remove<T>(string key) where T : class
//    {
//        var count = await this.ExecuteNonQueryAsync($"DELETE FROM {typeof(T).Name} WHERE Id = @Id").ConfigureAwait(false); // TODO: parameter
//        return count > 0;
//    }


//    public Task<bool> Set(string key, object entity) => throw new NotImplementedException();



//    public void CreateEntry(Type type)
//    {
//        // TODO: create a map
//        var sql = $@"CREATE TABLE IF NOT EXISTS {type.Name}(Id TEXT PRIMARY KEY, ";
//        var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite).ToList();

//        foreach (var property in properties)
//        {
//            if (!sql.EndsWith(","))
//                sql += ",";
//            sql += property.Name + " data_type NOT NULL DEFAULT 0";
//        }

//        sql += ")";
//        this.ExecuteNonQuery(sql);
//    }


//    string GetTableType(Type type)
//    {
//        if (type == typeof(string))
//            return "TEXT";

//        if (type == typeof(int))
//            return "INTEGER";

//        if (type == typeof(bool))
//            return "INTEGER";

//        if (type == typeof(long))
//            return "INTEGER";

//        if (type == typeof(float))
//            return "REAL";

//        if (type == typeof(double))
//            return "REAL";

//        if (type == typeof(byte[]))
//            return "BLOB";

//        throw new InvalidOperationException("Invalid Store Type - " + type.FullName);
//    }


//    // TODO: need update/insert

//    async Task<List<T>> GetFromDatabase<T>() where T : class, new()
//    {
//        var list = new List<T>();
//        // TODO: orderby, expression
//        using var conn = new SqliteConnection(this.connectionString);
//        using var cmd = conn.CreateCommand();

//        cmd.CommandText = $"SELECT * FROM {typeof(T).Name}";
//        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
//        while (reader.Read())
//        {
//            var obj = new T();
//            list.Add(obj);
//        }
//        return list;
//    }

//    void ExecuteNonQuery(string sql)
//    {
//        using var conn = new SqliteConnection(this.connectionString);
//        using var cmd = conn.CreateCommand();

//        cmd.CommandText = sql;
//        conn.Open();
//        cmd.ExecuteNonQuery();
//    }


//    async Task<int> ExecuteNonQueryAsync(string sql, params SqliteParameter[] parameters)
//    {
//        using var conn = new SqliteConnection(this.connectionString);
//        using var cmd = conn.CreateCommand();
//        cmd.CommandText = sql;

//        // TODO: parameters
//        await conn.OpenAsync().ConfigureAwait(false);
//        return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
//    }
//}

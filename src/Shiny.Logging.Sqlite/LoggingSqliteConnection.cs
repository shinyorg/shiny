using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Shiny.Logging.Sqlite;


public class LogStore
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }
    
    public string Message { get; set; }
    public int EventId { get; set; }
    // public string? Parameters { get; set; }
    public LogLevel LogLevel { get; set; }
    public DateTime TimestampUtc { get; set; }
}


public class LoggingSqliteConnection : SQLiteAsyncConnection
{
    private LoggingSqliteConnection(string path) : base(path)
    {
        this.GetConnection().CreateTable<LogStore>();
    }


    internal static LoggingSqliteConnection CreateInstance(string dbPath)
    {
        Instance = new LoggingSqliteConnection(dbPath);
        return Instance;
    }
    
    
    public static LoggingSqliteConnection? Instance { get; private set; }
    public AsyncTableQuery<LogStore> Logs => this.Table<LogStore>();

    public Task ClearLogs(Expression<Func<LogStore, bool>>? expression = null)
    {
        if (expression != null)
            return this.Logs.DeleteAsync(expression);
        
        return this.DeleteAllAsync<LogStore>();
    }
}

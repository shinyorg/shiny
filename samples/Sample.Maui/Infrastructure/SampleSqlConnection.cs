using SQLite;

namespace Sample.Infrastructure;


public class SampleSqlConnection : SQLiteAsyncConnection
{
    public SampleSqlConnection(IPlatform platform) : base(Path.Combine(platform.AppData.FullName, "sample.db"))
    {
        var conn = this.GetConnection();
        var result = conn.CreateTable<Log>();
    }


    public Task Log(string detail) => this.InsertAsync(new Log
    {
        Detail = detail,
        Timestamp = DateTimeOffset.UtcNow
    });
    public AsyncTableQuery<Log> Logs => this.Table<Log>();
}


public class Log
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }


    public string Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
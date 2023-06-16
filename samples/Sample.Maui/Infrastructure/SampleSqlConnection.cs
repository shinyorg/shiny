using SQLite;

namespace Sample.Infrastructure;


public class SampleSqliteConnection : SQLiteAsyncConnection
{
    public SampleSqliteConnection(IPlatform platform) : base(Path.Combine(platform.AppData.FullName, "sample.db"))
    {
        var conn = this.GetConnection();
        var result = conn.CreateTable<Log>();
    }


    public Task Log(string category, string text, string? detail = null, DateTimeOffset? timestamp = null) => this.InsertAsync(new Log
    {
        Category = category,
        Text = text,
        Detail = detail,
        Timestamp = timestamp ?? DateTimeOffset.UtcNow
    });
    public AsyncTableQuery<Log> Logs => this.Table<Log>();
}


public class Log
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    public string Category { get; set; }
    public string Text { get; set; }
    public string Detail { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
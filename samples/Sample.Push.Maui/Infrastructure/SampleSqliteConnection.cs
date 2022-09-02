using System.IO;
using SQLite;

namespace Sample;


public class SampleSqliteConnection : SQLiteAsyncConnection
{
    // because this is dependency injected with Shiny, we can use Shiny's platform implementation to get the proper appdata path to store our database
    public SampleSqliteConnection(IPlatform platform) : base(Path.Combine(platform.AppData.FullName, "sample.db"))
    {
        this.GetConnection().CreateTable<ShinyEvent>();
    }


    public AsyncTableQuery<ShinyEvent> Events => this.Table<ShinyEvent>();
}

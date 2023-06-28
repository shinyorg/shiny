using System.IO;
using Microsoft.Extensions.Logging;
using Shiny.Logging.Sqlite;

namespace Shiny.Tests.Logging;


public class SqliteLoggingTests : AbstractShinyTests
{
    public SqliteLoggingTests(ITestOutputHelper output) : base(output) {}


    protected override void Configure(HostBuilder hostBuilder)
    {
        base.Configure(hostBuilder);
        hostBuilder.Logging.AddSqlite();
    }


    public override void Dispose()
    {
        base.Dispose();
        var conn = LoggingSqliteConnection.Instance!.GetConnection();
        conn.Close();
        File.Delete(conn.DatabasePath);
    }


    [Fact(DisplayName = "Logging - SQLite - End-to-End")]
    public async Task Test()
    {
        var logger = this.GetService<ILogger<SqliteLoggingTests>>();

        logger.LogTrace("Info Test");
        logger.LogDebug("Debug Test");
        logger.LogWarning("Warning Test");
        logger.LogInformation("Info Test");
        logger.LogError(new InvalidProgramException("Error Exception"), "Error Test");
        logger.LogCritical(new InvalidProgramException("Critical Exception"), "Critical Test");

        var logs = await LoggingSqliteConnection.Instance!.Logs.ToListAsync();
        logs.Count.Should().Be(6);
    }
}
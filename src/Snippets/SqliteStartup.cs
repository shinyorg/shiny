using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class SqliteStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseSqliteLogging(true, false);

        services.UseSqliteSettings();

        services.UseSqliteStorage();
    }
}

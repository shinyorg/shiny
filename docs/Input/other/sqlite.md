Title: SQLite Integrations
---


Install from NuGet: <?# NugetShield "Shiny.Integrations.Sqlite" /?>

The SQLite integration provides several overrides to the built-in modules within Shiny.  

* Service Storage
* Key/Value Storage
* Logging

## Registering Them

For the most part, you won't see SQLite after you register it in your Shiny startup.  You are simply swapping "engines" for the "under the hood" stuff.  To use the functionality, in your Shiny startup file, add the following

```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class SqliteStartup : ShinyStartup
{
    public override void ConfigureLogging(ILoggingBuilder builder, IPlatform platform)
    {
        builder.UseSqliteLogging();
    }


    public override void ConfigureServices(IServiceCollection services, IPlatform platform)
    {
        services.UseSqliteSettings();
        services.UseSqliteStorage();
    }
}

```

## Why Swap with SQLite

There are 3 main points to consider when swapping in SQLite
1. If you are using SQLite Crypto - you will gain data protection across all of the different function points.  
2. Performance is not necessarily going to be better and in some cases will be worse.  
3. Data persistence will last across sessions where in-memory providers (cache) are used.

|Feature|Benefit(s)|Sacrifice(s)
|-------|-----|
|Storage|The benefit here is that you can use SQLite encryption to potentially protect values|
|Settings|Same benefits of potential encryption.  However, if you are doing frequent (many changes in a second) writes to settings, you will lose performance|
|Logging|There is no performance benefits to using this, however, it does centralize the logs for shipping purposes if you want|

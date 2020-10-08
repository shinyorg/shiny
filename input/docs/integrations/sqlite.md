<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/integrations/sqlite.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: SQLite
---
# INTEGRATIONS - SQLite

Install from NuGet: [![NuGet](https://img.shields.io/nuget/v/Shiny.Integrations.Sqlite.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Integrations.Sqlite/)

The SQLite integration provides several overrides to the built-in modules within Shiny.  

* Storage
* Settings
* Logging

## Registering Them

For the most part, you won't see SQLite after you register it in your Shiny startup.  You are simply swapping "engines" for the "under the hood" stuff.  To use the functionality, in your Shiny startup file, add the following

<!-- snippet: SqliteStartup.cs -->
```cs
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

```
<sup>[snippet source](/src/Snippets/SqliteStartup.cs#L1-L15)</sup>
<!-- endsnippet -->


## Why Swap with SQLite

There are 3 main points to consider when swapping in SQLite
1. If you are using SQLite Crypto - you will gain data protection across all of the different function points.  
2. Performance is not necessarily going to be better and in some cases will be worse.  
3. Data persistence will last across sessions where in-memory providers (cache) are used.

|Feature|Benefit(s)|Sacrific(s)
|-------|-----|
|Storage|The benefit here is that you can use SQLite encryption to potentially protect values|
|Settings|Same benefits of potential encryption.  However, if you are doing frequent (many changes in a second) writes to settings, you will lose performance|
|Logging|There is no performance benefits to using this, however, it does centralize the logs for shipping purposes if you want|

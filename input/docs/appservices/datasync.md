<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/appservices/datasync.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: Data Sync
Order: 3
---

<!-- snippet: DataSyncDelegate.cs -->
```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.DataSync.Infrastructure;


namespace Shiny.DataSync
{
    public interface IDataSyncDelegate
    {
        Task Push(SyncItem item);
    }
}

```
<sup>[snippet source](/src/Shiny.DataSync/IDataSyncDelegate.cs#L1-L14)</sup>
```cs
using System.Threading.Tasks;
using Shiny.DataSync;

public class DataSyncDelegate : IDataSyncDelegate
{
    public async Task Push(SyncItem item)
    {
        // your can inject your http service in the constructor
        // and call it here.  This method will only be called when a network connection
        // is seen.  Do not trap errors here, errors tell Shiny to try this item again in the next run
    }
}

```
<sup>[snippet source](/src/Snippets/DataSyncDelegate.cs#L1-L13)</sup>
<!-- endsnippet -->

<!-- snippet: DataSyncStartup.cs -->
```cs
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class DataSyncStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseDataSync<DataSyncDelegate>();
    }
}
```
<sup>[snippet source](/src/Snippets/DataSyncStartup.cs#L1-L11)</sup>
<!-- endsnippet -->

<!-- snippet: DataSyncUsage.cs -->
```cs
using System.Threading.Tasks;
using Shiny;
using Shiny.DataSync;

public class YourEntity : ISyncEntity
{
    public string EntityId { get; set; }
}

public class DataSyncUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IDataSyncManager>();

        await manager.Save(new YourEntity { EntityId = "Hello1" }, SyncOperation.Create);
        await manager.Save(new YourEntity { EntityId = "Hello2" }, SyncOperation.Update);
        await manager.Save(new YourEntity { EntityId = "Hello3" }, SyncOperation.Delete);

        await manager.ForceRun();

        await manager.ClearPending();
    }
}
```
<sup>[snippet source](/src/Snippets/DataSyncUsage.cs#L1-L24)</sup>
<!-- endsnippet -->

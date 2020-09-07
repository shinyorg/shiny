Title: Data Sync
Order: 3
---

<!-- snippet: DataSyncDelegate.cs -->
<a id='snippet-DataSyncDelegate.cs'></a>
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
<sup><a href='/src/Snippets/DataSyncDelegate.cs#L1-L12' title='File snippet `DataSyncDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-DataSyncDelegate.cs' title='Navigate to start of snippet `DataSyncDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: DataSyncStartup.cs -->
<a id='snippet-DataSyncStartup.cs'></a>
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
<sup><a href='/src/Snippets/DataSyncStartup.cs#L1-L11' title='File snippet `DataSyncStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-DataSyncStartup.cs' title='Navigate to start of snippet `DataSyncStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: DataSyncUsage.cs -->
<a id='snippet-DataSyncUsage.cs'></a>
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
<sup><a href='/src/Snippets/DataSyncUsage.cs#L1-L24' title='File snippet `DataSyncUsage.cs` was extracted from'>snippet source</a> | <a href='#snippet-DataSyncUsage.cs' title='Navigate to start of snippet `DataSyncUsage.cs`'>anchor</a></sup>
<!-- endSnippet -->

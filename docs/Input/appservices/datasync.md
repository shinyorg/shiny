Title: Data Sync
---

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
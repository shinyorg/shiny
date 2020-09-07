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
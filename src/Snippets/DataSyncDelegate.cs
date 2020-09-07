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

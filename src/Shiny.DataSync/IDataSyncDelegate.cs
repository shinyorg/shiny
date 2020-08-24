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

using System;
using System.Threading.Tasks;


namespace Shiny.DataSync
{
    public interface IDataSyncDelegate
    {
        Task Push();
        Task Pull();
    }
}

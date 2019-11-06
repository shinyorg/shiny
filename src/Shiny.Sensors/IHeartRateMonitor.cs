using System;
using System.Threading.Tasks;


namespace Shiny.Sensors
{
    public interface IHeartRateMonitor : ISensor<ushort>
    {
        Task<AccessState> RequestAccess();
    }
}

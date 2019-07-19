using System;
using System.Threading.Tasks;
using Android.Hardware;

namespace Shiny.Sensors
{
    public class HeartRateMonitorImpl : AbstractSensor<ushort>, IHeartRateMonitor
    {
        public HeartRateMonitorImpl() : base(SensorType.HeartRate) {}
        public Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);
        protected override ushort ToReading(SensorEvent e) => Convert.ToUInt16(e.Values[0]);
    }
}

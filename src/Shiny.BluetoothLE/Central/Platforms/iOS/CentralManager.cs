using System;


namespace Shiny.BluetoothLE.Central
{
    public partial class CentralManager : AbstractCentralManager
    {
        public CentralManager(IServiceProvider services, BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(services, config);
        }
	}
}
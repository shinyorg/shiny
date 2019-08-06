using System;


namespace Acr.BluetoothLE.Central
{
    public partial class CentralManager : AbstractCentralManager
    {
        public CentralManager(BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(config);
        }
	}
}
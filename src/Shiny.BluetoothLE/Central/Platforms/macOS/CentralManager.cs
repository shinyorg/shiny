using System;


namespace Acr.BluetoothLE.Central
{
    public partial class CentralManager : AbstractCentralManager
    {
        public CentralManager(ServiceProvider services, BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(services, config);
        }


        public override BleFeatures Features => BleFeatures.None;
	}
}
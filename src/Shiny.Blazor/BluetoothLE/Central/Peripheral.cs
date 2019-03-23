using System;


namespace Acr.BluetoothLE.Central
{
    public class Peripheral : AbstractPeripheral
    {
        public override object NativeDevice { get; }
        public override ConnectionState Status { get; }
        public override void Connect(ConnectionConfig config)
        {
            throw new NotImplementedException();
        }


        public override void CancelConnection()
        {
            throw new NotImplementedException();
        }


        public override IObservable<ConnectionState> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IGattService> DiscoverServices()
        {
            throw new NotImplementedException();
        }
    }
}

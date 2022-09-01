using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public class Peripheral : IPeripheral
{
    public Peripheral(string uuid, string name)
    {
        this.Uuid = uuid;
        this.Name = name;
    }

    public string Name { get; }
    public string Uuid { get; }

    public ConnectionState Status => throw new NotImplementedException();

    public int MtuSize => throw new NotImplementedException();

    public void CancelConnection() => throw new NotImplementedException();
    public void Connect(ConnectionConfig? config = null) => throw new NotImplementedException();
    public IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false) => throw new NotImplementedException();
    public IObservable<IList<IGattService>> GetServices() => throw new NotImplementedException();
    public IObservable<int> ReadRssi() => throw new NotImplementedException();
    public IObservable<BleException> WhenConnectionFailed() => throw new NotImplementedException();
    public IObservable<string> WhenNameUpdated() => throw new NotImplementedException();
    public IObservable<ConnectionState> WhenStatusChanged() => throw new NotImplementedException();
}


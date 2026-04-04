using System;
using System.Collections.Generic;
using System.Reactive;

namespace Shiny.BluetoothLE;


public class Peripheral : IPeripheral
{
    public Peripheral(string uuid, string? name)
    {
        this.Uuid = uuid;
        this.Name = name;
    }

    public string? Name { get; }
    public string Uuid { get; }
    public ConnectionState Status => throw new NotImplementedException("Web Bluetooth connection management is not yet implemented");
    public int Mtu => throw new NotImplementedException("Web Bluetooth MTU is not yet implemented");

    public void CancelConnection() => throw new NotImplementedException("Web Bluetooth connection management is not yet implemented");
    public void Connect(ConnectionConfig? config = null) => throw new NotImplementedException("Web Bluetooth connection management is not yet implemented");
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
    public IObservable<int> ReadRssi() => throw new NotImplementedException("Web Bluetooth RSSI is not yet implemented");
    public IObservable<BleException> WhenConnectionFailed() => throw new NotImplementedException("Web Bluetooth connection management is not yet implemented");
    public IObservable<ConnectionState> WhenStatusChanged() => throw new NotImplementedException("Web Bluetooth connection management is not yet implemented");
    public IObservable<Unit> WhenServicesChanged() => throw new NotImplementedException("Web Bluetooth GATT is not yet implemented");
}

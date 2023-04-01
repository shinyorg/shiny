using System;
using System.Collections.Generic;
using System.Reactive;

namespace Shiny.BluetoothLE;


public record BleServiceInfo(string Uuid);
public record BleCharacteristicInfo(
    string ServiceUuid,
    string Uuid,
    CharacteristicProperties Properties
);
public record BleDescriptorInfo(
    string ServiceUuid,
    string CharacteristicUuid,
    string Uuid
);
public record BleCharacteristicResult(
    string ServiceUuid,
    string Uuid,
    byte[]? Data
);

public interface IPeripheral
{
    string Uuid { get; }
    string? Name { get; }
    int Mtu { get; }
    ConnectionState Status { get; } // TODO: add failed state?

    void Connect(ConnectionConfig? config);
    void CancelConnection();
    IObservable<ConnectionState> WhenStatusChanged();
    IObservable<int> ReadRssi();

    IObservable<BleServiceInfo> GetService(string serviceUuid);
    IObservable<IReadOnlyList<BleServiceInfo>> GetServices();

    IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid);
    IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid);

    bool IsNotifying(string serviceUuid, string characteristicUuid);
    IObservable<BleCharacteristicInfo> WhenNotificationHooked();
    IObservable<BleCharacteristicResult> WhenNotification(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true);

    IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid);
    IObservable<Unit> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true);

    IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid);
    IObservable<byte[]> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);
    IObservable<Unit> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data);
}
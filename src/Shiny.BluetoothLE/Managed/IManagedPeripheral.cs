using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive.Concurrency;

namespace Shiny.BluetoothLE.Managed;


public interface IManagedPeripheral : INotifyPropertyChanged, IDisposable
{
    IReadOnlyList<GattCharacteristicInfo> Characteristics { get; }
    bool IsMonitoringRssi { get; }
    string Name { get; }
    IPeripheral Peripheral { get; }
    int? Rssi { get; }
    IScheduler? Scheduler { get; set; }
    ConnectionState Status { get; }

    void CancelConnection();
    IObservable<IManagedPeripheral> ConnectWait(ConnectionConfig? config = null);
    IObservable<byte[]?> Read(string serviceUuid, string characteristicUuid);
    void StartRssi();
    void StopRssi();
    bool ToggleRssi();
    IObservable<byte[]?> WhenNotificationReceived(string serviceUuid, string characteristicUuid, bool useIndicationIfAvailable = false, Action? onReady = null);
    IObservable<IManagedPeripheral> Write(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true);
    IObservable<IManagedPeripheral> WriteBlob(string serviceUuid, string characteristicUuid, Stream stream);
}
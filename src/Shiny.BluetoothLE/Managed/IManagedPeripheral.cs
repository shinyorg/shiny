using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;


namespace Shiny.BluetoothLE.Managed
{
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
        IObservable<ManagedPeripheral> ConnectWait(ConnectionConfig? config = null);
        IObservable<Unit> EnableNotifications(bool enable, string serviceUuid, string characteristicUuid, bool useIndicationIfAvailable = false);
        IObservable<byte[]?> Read(string serviceUuid, string characteristicUuid);
        void StartRssi();
        void StopRssi();
        bool ToggleRssi();
        IObservable<GattCharacteristicResult> WhenAnyNotificationReceived();
        IObservable<(string ServiceUuid, string CharacteristicUuid)> WhenNotificationReady();
        IObservable<byte[]> WhenNotificationReceived(string serviceUuid, string characteristicUuid);
        IObservable<ManagedPeripheral> Write(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true);
        IObservable<ManagedPeripheral> WriteBlob(string serviceUuid, string characteristicUuid, Stream stream);
    }
}
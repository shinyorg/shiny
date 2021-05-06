using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace Shiny.BluetoothLE.Managed
{

    // TODO: general purpose error subject for restore fails?
    public class ManagedPeripheral : NotifyPropertyChanged, IDisposable, IManagedPeripheral
    {
        readonly List<GattCharacteristicInfo> characteristics;
        readonly Subject<GattCharacteristicResult> notifySub;
        readonly Subject<(string ServiceUuid, string CharacteristicUuid)> notifyReadySub;
        CompositeDisposable npcDispose;
        CompositeDisposable coreDispose;
        CompositeDisposable? notifyDispose;
        IDisposable? rssiSub;


        public ManagedPeripheral(IPeripheral peripheral, IScheduler? scheduler = null)
        {
            this.coreDispose = new CompositeDisposable();
            this.characteristics = new List<GattCharacteristicInfo>();
            this.notifySub = new Subject<GattCharacteristicResult>();
            this.notifyReadySub = new Subject<(string ServiceUuid, string CharacteristicUuid)>();
            this.Peripheral = peripheral;

            this.CreateNpc(scheduler);
            this.Peripheral
                .WhenConnected()
                .Do(_ => this.notifyDispose = new CompositeDisposable())
                .Select(_ => this.RestoreNotifications())
                .Subscribe()
                .DisposedBy(this.coreDispose);

            this.Peripheral
                .WhenDisconnected()
                .Do(_ =>
                {
                    this.notifyDispose?.Dispose();
                    lock (this.characteristics)
                    {
                        foreach (var ch in this.characteristics)
                            ch.Characteristic = null;
                    }
                })
                .Subscribe()
                .DisposedBy(this.coreDispose);
        }


        /// <summary>
        /// This is the raw peripheral
        /// </summary>
        public IPeripheral Peripheral { get; }


        public IObservable<ManagedPeripheral> ConnectWait(ConnectionConfig? config = null) => this.Peripheral
            .WithConnectIf(config)
            .Select(_ => this);

        public void CancelConnection() => this.Peripheral.CancelConnection();


        IScheduler? scheduler;
        public IScheduler? Scheduler
        {
            get => this.scheduler;
            set
            {
                this.scheduler = value;
                this.CreateNpc(value);
            }
        }


        string name;
        public string Name
        {
            get => this.name;
            private set => this.Set(ref this.name, value);
        }


        ConnectionState status;
        public ConnectionState Status
        {
            get => this.status;
            private set => this.Set(ref this.status, value);
        }


        int? rssi;
        public int? Rssi
        {
            get => this.rssi;
            private set => this.Set(ref this.rssi, value);
        }


        public bool IsMonitoringRssi => this.rssiSub != null;

        public IObservable<byte[]> WhenNotificationReceived(string serviceUuid, string characteristicUuid) =>
            this.notifySub
                .Where(x =>
                    x.Characteristic.Service.Uuid.Equals(serviceUuid) &&
                    x.Characteristic.Uuid.Equals(characteristicUuid)
                )
                .Select(x => x.Data!);

        public IObservable<GattCharacteristicResult> WhenAnyNotificationReceived()
            => this.notifySub;

        public IObservable<(string ServiceUuid, string CharacteristicUuid)> WhenNotificationReady()
            => this.notifyReadySub;


        public IReadOnlyList<GattCharacteristicInfo> Characteristics => this.characteristics.AsReadOnly();


        public IObservable<ManagedPeripheral> WriteBlob(string serviceUuid, string characteristicUuid, Stream stream) => this
            .GetChar(serviceUuid, characteristicUuid)
            .Select(x => x.WriteBlob(stream))
            .Select(_ => this);


        public IObservable<ManagedPeripheral> Write(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
            .GetChar(serviceUuid, characteristicUuid)
            .Select(x => x.Write(data, withResponse))
            .Select(x =>
            {
                this.GetInfo(serviceUuid, characteristicUuid).Value = data;
                return this;
            });


        public IObservable<byte[]?> Read(string serviceUuid, string characteristicUuid) => this
            .GetChar(serviceUuid, characteristicUuid)
            .Select(x => x.Read())
            .Switch()
            .Select(x =>
            {
                this.GetInfo(serviceUuid, characteristicUuid).Value = x.Data;
                return x.Data;
            });


        public IObservable<Unit> EnableNotifications(bool enable, string serviceUuid, string characteristicUuid, bool useIndicationIfAvailable = false) =>
            this.GetChar(serviceUuid, characteristicUuid)
                .Select(x => x.EnableNotifications(enable, useIndicationIfAvailable))
                .Switch()
                .Do(_ =>
                {
                    var info = this.GetInfo(serviceUuid, characteristicUuid);
                    info.IsNotificationsEnabled = enable;
                    info.UseIndicationIfAvailable = useIndicationIfAvailable;
                })
                .Select(_ => Unit.Default);


        public bool ToggleRssi()
        {
            if (this.IsMonitoringRssi)
                this.StopRssi();
            else
                this.StartRssi();

            return this.IsMonitoringRssi;
        }


        public void StartRssi()
        {
            this.rssiSub = this.Peripheral
                .ReadRssiContinuously()
                .ObserveOnIf(this.Scheduler)
                .Subscribe(x => this.Rssi = x);
        }


        public void StopRssi()
        {
            this.rssiSub?.Dispose();
            this.rssiSub = null;
        }


        IObservable<IGattCharacteristic> GetChar(string serviceUuid, string characteristicUuid) => this.Peripheral
            .WithConnectIf()
            .Select(p =>
            {
                var info = this.GetInfo(serviceUuid, characteristicUuid);
                if (info.Characteristic != null)
                    return Observable.Return(info.Characteristic);

                return p
                    .GetKnownCharacteristic(serviceUuid, characteristicUuid, true)
                    .Do(x => info.Characteristic = x);
            })
            .Switch()!;


        IObservable<Unit> RestoreNotifications() => this.characteristics
            .ToObservable()
            .Where(x => x.IsNotificationsEnabled)
            .Select(x => this
                .GetChar(x.ServiceUuid, x.CharacteristicUuid)
                .Do(ch => ch
                    .WhenNotificationReceived()
                    .Subscribe(x => this.notifySub.OnNext(x))
                    .DisposedBy(this.notifyDispose!)
                )
                .Select(y => y.EnableNotifications(true, x.UseIndicationIfAvailable))
                .Switch()
            )
            .Switch()
            .Do(x => this.notifyReadySub.OnNext((x.Service.Uuid, x.Uuid)))
            .Select(_ => Unit.Default);


        GattCharacteristicInfo GetInfo(string serviceUuid, string characteristicUuid)
        {
            var ch = this.characteristics.FirstOrDefault(x => x.Equals(serviceUuid, characteristicUuid));

            if (ch == null)
            {
                ch = new GattCharacteristicInfo(serviceUuid, characteristicUuid);
                lock (this.characteristics)
                    this.characteristics.Add(ch);
            }
            return ch;
        }


        void CreateNpc(IScheduler? scheduler)
        {
            this.npcDispose?.Dispose();
            this.npcDispose = new CompositeDisposable();

            this.Peripheral
                .WhenStatusChanged()
                .ObserveOnIf(scheduler)
                .Subscribe(x => this.Status = x)
                .DisposedBy(this.npcDispose);

            this.Peripheral
                .WhenNameUpdated()
                .ObserveOnIf(scheduler)
                .StartWith(this.Peripheral.Name)
                .Subscribe(x => this.Name = x)
                .DisposedBy(this.npcDispose);
        }


        public void Dispose()
        {
            this.Peripheral.CancelConnection();
            this.npcDispose.Dispose();
            this.coreDispose.Dispose();
            this.StopRssi();
        }
    }
}

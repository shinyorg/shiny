using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace Shiny.BluetoothLE.Managed
{

    public class ManagedPeripheral : NotifyPropertyChanged, IDisposable, IManagedPeripheral
    {
        readonly List<GattCharacteristicInfo> characteristics;
        readonly ILogger logger;
        CompositeDisposable npcDispose;
        CompositeDisposable coreDispose;
        IDisposable? rssiSub;


        public ManagedPeripheral(IPeripheral peripheral, IScheduler? scheduler = null)
        {
            this.logger = ShinyHost.LoggerFactory.CreateLogger<IManagedPeripheral>();

            this.coreDispose = new CompositeDisposable();
            this.characteristics = new List<GattCharacteristicInfo>();
            this.Peripheral = peripheral;
            this.CreateNpc(scheduler);

            this.Peripheral
                .WhenDisconnected()
                .Do(_ =>
                {
                    lock (this.characteristics)
                    {
                        foreach (var ch in this.characteristics)
                        {
                            // these will be restored by WhenConnected on each observable
                            ch.IsNotificationsEnabled = false;
                            ch.UseIndicationIfAvailable = false;
                            ch.Characteristic = null;
                        }
                    }
                })
                .Subscribe()
                .DisposedBy(this.coreDispose);
        }


        /// <summary>
        /// This is the raw peripheral
        /// </summary>
        public IPeripheral Peripheral { get; }


        public IObservable<IManagedPeripheral> ConnectWait(ConnectionConfig? config = null) => this.Peripheral
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

        // TODO: how to fire multiple whenReadys, doonce? // finally could remove whenReady?
        public IObservable<byte[]?> WhenNotificationReceived(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvalable, Action? whenReady) =>
            this.GetNotificationObservable(serviceUuid, characteristicUuid, useIndicationsIfAvalable);


        readonly Dictionary<string, IObservable<byte[]?>> observables = new Dictionary<string, IObservable<byte[]?>>();
        protected IObservable<byte[]?> GetNotificationObservable(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvalable)
        {
            var key = $"{serviceUuid}-{characteristicUuid}";
            if (!this.observables.ContainsKey(key))
            {
                lock (this.observables)
                {
                    this.observables[key] ??= this.Peripheral
                        .WhenConnected()
                        .Select(x => this.GetChar(serviceUuid, characteristicUuid))
                        .Switch()
                        .Select(x => x.EnableNotifications(true, useIndicationsIfAvalable))
                        .Switch()
                        .Select(x =>
                        {
                            var ch = this.GetInfo(serviceUuid, characteristicUuid);
                            ch.IsNotificationsEnabled = true;
                            ch.UseIndicationIfAvailable = useIndicationsIfAvalable;

                            //whenReady?.Invoke();
                            return x.WhenNotificationReceived();
                        })
                        .Switch()
                        .Select(x => x.Data)
                        .Finally(async () =>
                        {
                            var ch = this.GetInfo(serviceUuid, characteristicUuid);
                            ch.IsNotificationsEnabled = false;
                            ch.UseIndicationIfAvailable = false;

                            if (ch.Characteristic != null)
                            {
                                try
                                {
                                    await ch.Characteristic.EnableNotifications(false).ToTask();
                                }
                                catch (Exception ex)
                                {
                                    this.logger.LogWarning("Unable to cleanly unhook peripheral", ex);
                                }
                            }
                        })
                        .Publish()
                        .RefCount();
                }
            }
            return this.observables[key];
        }


        public IReadOnlyList<GattCharacteristicInfo> Characteristics => this.characteristics.AsReadOnly();


        public IObservable<IManagedPeripheral> WriteBlob(string serviceUuid, string characteristicUuid, Stream stream) => this
            .GetChar(serviceUuid, characteristicUuid)
            .Select(x => x.WriteBlob(stream))
            .Switch()
            .Select(_ => this);


        public IObservable<IManagedPeripheral> Write(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
            .GetChar(serviceUuid, characteristicUuid)
            .Select(x => x.Write(data, withResponse))
            .Switch()
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

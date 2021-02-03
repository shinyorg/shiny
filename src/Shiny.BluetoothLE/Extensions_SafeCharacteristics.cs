using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public class SafeCharacteristic : IGattCharacteristic, IDisposable
    {
        readonly IPeripheral peripheral;
        readonly CompositeDisposable composite;
        IGattCharacteristic? current;
        bool notify;
        bool notifyReady;
        bool useIndications;


        public SafeCharacteristic(IPeripheral peripheral, IGattCharacteristic initial)
        {
            this.peripheral = peripheral;
            this.Service = initial.Service;
            this.Uuid = initial.Uuid;
            this.Properties = initial.Properties;
            this.composite = new CompositeDisposable();
            this.peripheral
                .WhenDisconnected()
                .Subscribe(_ =>
                {
                    this.current = null;
                    this.notifyReady = false;
                })
                .DisposedBy(this.composite);

            // TODO: need to broadcast errors on re-enabling
            this.peripheral
                .WhenConnected()
                .Where(_ => this.IsNotifying)
                .Select(x => this.EnableNotifications(true, this.useIndications))
                .Subscribe()
                .DisposedBy(this.composite);
        }


        public bool IsReady => this.current != null;
        public IGattService Service { get; private set; }
        public string Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public bool IsNotifying => this.notify && this.notifyReady;
        public IObservable<IGattDescriptor> DiscoverDescriptors() => this.Get().Select(x => x.DiscoverDescriptors()).Switch();
        public IObservable<Unit> EnableNotifications(bool enable, bool useIndicationIfAvailable = false) => this
            .Get()
            .Select(x => x.EnableNotifications(enable, useIndicationIfAvailable))
            .Do(x =>
            {
                this.useIndications = useIndicationIfAvailable;
                this.notify = enable;
                this.notifyReady = enable;
            })
            .Switch();

        public IObservable<CharacteristicGattResult> Read() => this.Get().Select(x => x.Read()).Switch();
        public IObservable<CharacteristicGattResult> WhenNotificationReceived() => this.Get().Select(x => x.WhenNotificationReceived()).Switch();
        public IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse = true) => this.Get().Select(x => x.Write(value, withResponse)).Switch();


        IObservable<IGattCharacteristic> Get() => Observable
            .Create<IGattCharacteristic>(ob =>
            {
                IDisposable? sub = null;
                if (this.current != null)
                {
                    ob.Respond(this.current);
                }
                else
                {
                    sub = this.peripheral
                        .GetKnownCharacteristic(this.Service.Uuid, this.Uuid)
                        .Subscribe(
                            x =>
                            {
                                this.Service = x.Service;
                                this.current = x;
                                ob.Respond(x);
                            },
                            ob.OnError
                        );
                }
                return () => sub?.Dispose();
            })
            .Synchronize();


        public void Dispose() => this.composite?.Dispose();
    }


    public static class SafeCharacteristicsExtensions
    {
        /// <summary>
        /// SafeCharacteristics allow you skip all of the "cruft" of having to re-find known characteristics per connection
        /// Under the hood, this will find the known characteristic upon reusing (or using a notification) automatically without having to clean up and re-find it
        /// PERF CONSIDERATION: since this initiates a scan for characteristic you use, it is recommended you only use this for small subsets ALSO ensure you dispose of it when you are done!
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<SafeCharacteristic> GetKnownSafeCharacteristic(this IPeripheral peripheral, string serviceUuid, string characteristicUuid)
            => peripheral
                .GetKnownCharacteristic(serviceUuid, characteristicUuid)
                .Select(x => new SafeCharacteristic(peripheral, x));
    }
}

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;
using Shiny.BluetoothLE.Hosting;


namespace Samples.BleHosting
{
    public class GattServerViewModel : ViewModel
    {
        const string LocalName = "ShinyTest";
        static readonly string ServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
        static readonly string Characteristic1Uuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";
        static readonly string Characteristic2Uuid = "A495FF22-C5B1-4B44-B512-1370F02D74DE";
        static readonly string Characteristic3Uuid = "A495FF23-C5B1-4B44-B512-1370F02D74DE";
        static readonly string Characteristic4Uuid = "A495FF24-C5B1-4B44-B512-1370F02D74DE";

        readonly IBleHostingManager peripheralManager;
        public GattServerViewModel(IBleHostingManager hostingManager)
            => this.peripheralManager = hostingManager;


        [Reactive] public string LastWriteValue { get; private set; }
        [Reactive] public string LastWriteTime { get; private set; }
        [Reactive] public string LastReadValue { get; private set; }
        [Reactive] public string LastReadTime { get; private set; }

        [Reactive] public int SubscribersSimple { get; private set; }
        [Reactive] public string SubscriberLastValue { get; private set; }

        [Reactive] public int SpeedWrites { get; private set; }
        [Reactive] public int SpeedReads { get; private set; }
        [Reactive] public string TransferSpeed { get; private set; }

        [Reactive] public int SubscribersPerf { get; private set; }
        [Reactive] public int SubscriberPushes { get; private set; }
        [Reactive] public string SubscriberSpeed { get; private set; }

        IGattCharacteristic simplePush;
        IGattCharacteristic perfPush;


        public override async void OnAppearing()
        {
            base.OnAppearing();

            var service = await peripheralManager.AddService(ServiceUuid, true, sb =>
            {
                sb.AddCharacteristic(Characteristic1Uuid, cb => cb
                    .SetWrite(request =>
                    {
                        this.LastWriteValue = Encoding.UTF8.GetString(request.Data, 0, request.Data.Length);
                        this.LastWriteTime = DateTime.Now.ToString();
                        return GattState.Success;
                    })
                    .SetRead(request =>
                    {
                        var ticks = DateTime.Now.Ticks;
                        this.LastReadValue = ticks.ToString();
                        this.LastReadTime = DateTime.Now.ToString();
                        var data = BitConverter.GetBytes(ticks);
                        return ReadResult.Success(data);
                    })
                );

                this.simplePush = sb.AddCharacteristic(Characteristic2Uuid, cb => cb.SetNotification(cs =>
                    // main thread
                    this.SubscribersSimple = cs.Characteristic.SubscribedCentrals.Count
                ));

                sb.AddCharacteristic(Characteristic3Uuid, cb => cb
                    .SetWrite(request =>
                    {
                        // main thread
                        this.SpeedWrites++;
                        return GattState.Success;
                    })
                    .SetRead(request =>
                    {
                        //Interlocked.Increment(ref this.SpeedWrites);
                        var data = BitConverter.GetBytes(DateTime.Now.Ticks);
                        return ReadResult.Success(data);
                    })
                );

                this.perfPush = sb.AddCharacteristic(Characteristic4Uuid, cb => cb.SetNotification(cs =>
                    this.SubscribersPerf = cs.Characteristic.SubscribedCentrals.Count
                ));
            });

            await this.peripheralManager.StartAdvertising(new AdvertisementData
            {
                LocalName = LocalName
            });

            // TODO: simple push of datetime
            Observable
                .Interval(TimeSpan.FromSeconds(2))
                .Select(_ => Observable.FromAsync(_ => this.Send()))
                .SubOnMainThread(x =>
                {
                    // TODO: calc speeds
                })
                .DisposeWith(this.DeactivateWith);
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.peripheralManager.ClearServices();
            this.peripheralManager.StopAdvertising();
        }


        async Task Send()
        {
            var ticks = DateTime.Now.Ticks;
            var data = BitConverter.GetBytes(ticks);
            this.SubscriberLastValue = ticks.ToString();
            await this.simplePush.Notify(data);
        }
    }
}

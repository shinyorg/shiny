using System;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Shiny;
using Shiny.BluetoothLE.Hosting;
using Xamarin.Forms;


namespace Sample
{
    public class MainViewModel : SampleViewModel
    {
        static readonly string ServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
        static readonly string Characteristic1Uuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";
        static readonly string Characteristic2Uuid = "A495FF22-C5B1-4B44-B512-1370F02D74DE";

        readonly IBleHostingManager hostingManager;
        IDisposable? notifierSub;
        IGattCharacteristic push;


        public MainViewModel()
        {
            this.hostingManager = ShinyHost.Resolve<IBleHostingManager>();
            this.ServerText = this.hostingManager.IsAdvertising ? "Stop Server" : "Start Server";

            this.ToggleServer = new Command(async () =>
            {
                this.hostingManager.ClearServices();

                if (this.hostingManager.IsAdvertising)
                {
                    this.hostingManager.StopAdvertising();
                    this.notifierSub?.Dispose();
                    this.ServerText = "Start Server";
                }
                else
                {
                    var service = await this.hostingManager.AddService(
                        ServiceUuid,
                        true,
                        this.BuildService
                    );

                    await this.hostingManager.StartAdvertising(new AdvertisementOptions
                    {
                        LocalName = this.LocalName,
                        AndroidIncludeDeviceName = this.AndroidIncludeDeviceName,
                        AndroidIncludeTxPower = this.AndroidIncludeTx,
                        UseGattServiceUuids = true
                    });

                    this.ServerText = "Stop Server";
                }
            });
        }


        public ICommand ToggleServer { get; }

        string serverText;
        public string ServerText
        {
            get => this.serverText;
            private set => this.Set(ref this.serverText, value);
        }


        string localName = "ShinyTest";
        public string LocalName
        {
            get => this.localName;
            set => this.Set(ref this.localName, value);
        }


        bool includeDeviceName = true;
        public bool AndroidIncludeDeviceName
        {
            get => this.includeDeviceName;
            set => this.Set(ref this.includeDeviceName, value);
        }


        bool includeTx = true;
        public bool AndroidIncludeTx
        {
            get => this.includeTx;
            set => this.Set(ref this.includeTx, value);
        }


        string lastWrite;
        public string LastWriteValue
        {
            get => this.lastWrite;
            private set => this.Set(ref this.lastWrite, value);
        }


        string writeTime;
        public string LastWriteTime
        {
            get => this.writeTime;
            private set => this.Set(ref this.writeTime, value);
        }


        string readValue;
        public string LastReadValue
        {
            get => this.readValue;
            private set => this.Set(ref this.readValue, value);
        }


        string readTime;
        public string LastReadTime
        {
            get => this.readTime;
            private set => this.Set(ref this.readTime, value);
        }


        int subscribers;
        public int Subscribers
        {
            get => this.subscribers;
            private set => this.Set(ref this.subscribers, value);
        }


        string subLastValue;
        public string SubscribersLastValue
        {
            get => this.subLastValue;
            private set => this.Set(ref this.subLastValue, value);
        }


        void BuildService(IGattServiceBuilder serviceBuilder)
        {
            serviceBuilder.AddCharacteristic(
                Characteristic1Uuid,
                cb =>
                {
                    cb.SetWrite(request =>
                    {
                        this.LastWriteValue = Encoding.UTF8.GetString(request.Data, 0, request.Data.Length);
                        this.LastWriteTime = DateTime.Now.ToString();
                        return GattState.Success;
                    });

                    cb.SetRead(request =>
                    {
                        var ticks = DateTime.Now.Ticks;
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            this.LastReadValue = ticks.ToString();
                            this.LastReadTime = DateTime.Now.ToString();
                        });
                        var data = BitConverter.GetBytes(ticks);
                        return ReadResult.Success(data);
                    });
                }
            );

            this.push = serviceBuilder.AddCharacteristic(
                Characteristic2Uuid,
                cb => cb.SetNotification(cs =>
                {
                    var c = cs.Characteristic.SubscribedCentrals.Count;
                    Device.BeginInvokeOnMainThread(() => this.Subscribers = c);

                    if (c == 0)
                    {
                        this.notifierSub?.Dispose();
                    }
                    else
                    {
                        this.notifierSub = Observable
                            .Interval(TimeSpan.FromSeconds(2))
                            .Select(_ => Observable.FromAsync(async () =>
                            {
                                var ticks = DateTime.Now.Ticks;
                                var data = BitConverter.GetBytes(ticks);
                                await this.push.Notify(data);

                                return ticks;
                            }))
                            .Subscribe(x => Device.BeginInvokeOnMainThread(() =>
                                this.SubscribersLastValue = x.ToString()
                            ));
                    }
                })
            );
        }
    }
}

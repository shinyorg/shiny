using System.Text;
using Shiny.BluetoothLE.Hosting;

namespace Sample.BleHosting;


public class MainViewModel : ViewModel
{
    static readonly string ServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
    static readonly string Characteristic1Uuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";
    static readonly string Characteristic2Uuid = "A495FF22-C5B1-4B44-B512-1370F02D74DE";

    readonly IBleHostingManager hostingManager;
    IDisposable? notifierSub;
    IGattCharacteristic push;


    public MainViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.hostingManager = hostingManager;
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
                    LocalName = this.LocalName
                    //AndroidIncludeDeviceName = this.AndroidIncludeDeviceName,
                    //AndroidIncludeTxPower = this.AndroidIncludeTx,
                    //UseGattServiceUuids = true
                });

                this.ServerText = "Stop Server";
            }
        });
    }


    public ICommand ToggleServer { get; }

    [Reactive] public string ServerText { get; private set; }
    [Reactive] public string LocalName { get; set; }
    [Reactive] public string LastWriteValue { get; private set; }
    [Reactive] public string LastWriteTime { get; private set; }
    [Reactive] public string LastReadValue { get; private set; }
    [Reactive] public string LastReadTime { get; private set; }
    [Reactive] public int Subscribers { get; private set; }
    [Reactive] public string SubscribersLastValue { get; private set; }


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
                    return Task.FromResult(GattState.Success);
                });

                cb.SetRead(request =>
                {
                    var ticks = DateTime.Now.Ticks;
                    this.Platform.InvokeOnMainThread(() =>
                    {
                        this.LastReadValue = ticks.ToString();
                        this.LastReadTime = DateTime.Now.ToString();
                    });
                    var data = BitConverter.GetBytes(ticks);
                    return Task.FromResult(GattResult.Success(data));
                });
            }
        );

        this.push = serviceBuilder.AddCharacteristic(
            Characteristic2Uuid,
            cb => cb.SetNotification(cs =>
            {
                var c = cs.Characteristic.SubscribedCentrals.Count;
                this.Platform.InvokeOnMainThread(() => this.Subscribers = c);

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
                        .SubOnMainThread(x =>
                            this.SubscribersLastValue = x.ToString()!
                        );
                }
                return Task.CompletedTask;
            })
        );
    }
}

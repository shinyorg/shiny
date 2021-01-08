using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Humanizer;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class PerformanceViewModel : ViewModel
    {
        const string DefaultServiceUuid = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
        const string DefaultCharacteristicUuid = "A495FF21-C5B1-4B44-B512-1370F02D74DE";

        readonly IBleManager centralManager;
        int bytes;
        IDisposable? notifySub;
        IDisposable? speedSub;


        public PerformanceViewModel(IBleManager centralManager, BleConfiguration configuration)
        {
            this.centralManager = centralManager;

            if (this.IsAndroid)
            {
                this.AndroidUseInternalSyncQueue = configuration.AndroidUseInternalSyncQueue;
                this.AndroidUseMainThread = configuration.AndroidShouldInvokeOnMainThread;

                this.WhenAnyValue(x => x.AndroidUseMainThread)
                    .Skip(1)
                    .Subscribe(x => configuration.AndroidShouldInvokeOnMainThread = x);

                this.WhenAnyValue(x => x.AndroidUseInternalSyncQueue)
                    .Skip(1)
                    .Subscribe(x => configuration.AndroidUseInternalSyncQueue = x);
            }

            this.WhenAnyValue(x => x.IsRunning)
                .Skip(1)
                .Subscribe(x =>
                {
                    if (!x)
                    {
                        this.speedSub?.Dispose();
                    }
                    else
                    {
                        this.speedSub = Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(_ =>
                        {
                            this.Speed = (this.bytes / 2).Bytes().Humanize("0.0");
                            Interlocked.Exchange(ref this.bytes, 0);
                        });
                    }
                });

            this.Permissions = ReactiveCommand.CreateFromTask(async () =>
                this.Status = await this.centralManager.RequestAccess().ToTask()
            );
            this.WriteTest = this.DoWrite(true);
            this.WriteWithoutResponseTest = this.DoWrite(false);
            this.ReadTest = this.DoWork("Read", async (ch, ct) =>
            {
                var read = await ch.Read().ToTask(ct);
                return read.Data?.Length ?? 0;
            });

            this.NotifyTest = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    this.IsRunning = true;
                    this.Errors = 0;
                    this.Packets = 0;

                    var characteristic = await this.SetupCharacteristic(this.cancelSrc.Token);
                    this.Info = "Running Notify Test";

                    this.notifySub = characteristic
                        .Notify(true)
                        .Where(x => x.Type == CharacteristicResultType.Notification)
                        .Subscribe(x =>
                        {
                            Interlocked.Add(ref this.bytes, x.Data?.Length ?? 0);
                            this.Packets++;
                        });
                },
                this.CanRun()
            );

            this.Stop = ReactiveCommand.Create(
                () =>
                {
                    this.IsRunning = false;
                    this.peripheral?.CancelConnection();
                    this.Info = "Test Stopped";
                    this.cancelSrc?.Cancel();
                    this.notifySub?.Dispose();
                    this.notifySub = null;
                },
                this.WhenAny(
                    x => x.IsRunning,
                    x => x.GetValue()
                )
            );
        }


        public ICommand WriteTest { get; }
        public ICommand WriteWithoutResponseTest { get; }
        public ICommand ReadTest { get; }
        public ICommand NotifyTest { get; }
        public ICommand Stop { get; }
        public ICommand Permissions { get; }

        public bool IsAndroid => Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.Android;
        [Reactive] public bool AndroidUseInternalSyncQueue { get; set; }
        [Reactive] public bool AndroidUseMainThread { get; set; }

        [Reactive] public string? PeripheralName { get; set; } = "ShinyTest";
        [Reactive] public bool IsConnected { get; private set; }
        [Reactive] public int MTU { get; private set; }
        [Reactive] public string? Speed { get; private set; }
        [Reactive] public string? ServiceUuid { get; set; } = DefaultServiceUuid;
        [Reactive] public string? CharacteristicUuid { get; set; } = DefaultCharacteristicUuid;
        [Reactive] public bool IsRunning { get; private set; }
        [Reactive] public string? Info { get; private set; }
        [Reactive] public int Packets { get; private set; }
        [Reactive] public int Errors { get; private set; }
        [Reactive] public AccessState Status { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Permissions.Execute(null);
        }


        ICommand DoWrite(bool withResponse) => this.DoWork(
            withResponse ? "Write With Response" : "Write W/O Response",
            async (ch, ct) =>
            {
                var data = Enumerable.Repeat<byte>(0x01, this.MTU).ToArray();
                await ch.Write(data, withResponse).ToTask(ct);
                return data.Length;
            }
        );


        CancellationTokenSource? cancelSrc;
        ICommand DoWork(string testName, Func<IGattCharacteristic, CancellationToken, Task<int>> func) => ReactiveCommand
            .CreateFromTask(async () =>
            {
                this.IsRunning = true;
                this.Errors = 0;
                this.Packets = 0;
                this.cancelSrc = new CancellationTokenSource();
                var characteristic = await this.SetupCharacteristic(this.cancelSrc.Token);
                this.Info = $"Running {testName} Test";

                while (!this.cancelSrc.IsCancellationRequested)
                {
                    try
                    {
                        var length = await func(characteristic, this.cancelSrc.Token);
                        Interlocked.Add(ref this.bytes, length);
                        this.Packets++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        this.Errors++;
                    }
                }
            },
            this.CanRun()
        );


        IPeripheral? peripheral;
        async Task<IGattCharacteristic> SetupCharacteristic(CancellationToken cancelToken)
        {
            try
            {
                this.Info = "Searching for peripheral..";

                this.peripheral = await this.centralManager
                    .ScanUntilPeripheralFound(this.PeripheralName.Trim())
                    .Timeout(TimeSpan.FromSeconds(5))
                    .ToTask(cancelToken);

                this.Info = "Peripheral Found - Connecting";

                await this.peripheral
                    .ConnectWait()
                    .Timeout(TimeSpan.FromSeconds(5))
                    .ToTask(cancelToken);

                this.Info = "Connected - Requesting MTU Change";

                if (this.peripheral is ICanRequestMtu mtu)
                    this.MTU = await mtu
                        .RequestMtu(512)
                        .ToTask(cancelToken);
                else
                    this.MTU = this.peripheral.MtuSize;

                this.Info = "Searching for characteristic";
                var characteristic = await this.peripheral
                    .GetKnownCharacteristic(
                        this.ServiceUuid,
                        this.CharacteristicUuid
                    )
                    .Timeout(TimeSpan.FromSeconds(5))
                    .ToTask(cancelToken);

                this.Info = "Characteristic Found";
                return characteristic;
            }
            catch
            {
                this.Info = "ERROR";
                this.IsRunning = false;
                throw;
            }
        }


        IObservable<bool> CanRun() => this.WhenAny(
            x => x.Status,
            x => x.PeripheralName,
            x => x.ServiceUuid,
            x => x.CharacteristicUuid,
            x => x.IsRunning,
            (stat, dn, suuid, cuuid, run) =>
                stat.GetValue() == AccessState.Available &&
                !dn.GetValue().IsEmpty() &&
                !suuid.GetValue().IsEmpty() &&
                !cuuid.GetValue().IsEmpty() &&
                !run.GetValue()
        );
    }
}

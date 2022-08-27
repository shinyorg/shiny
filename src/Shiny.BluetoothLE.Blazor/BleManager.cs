//using System.Reactive.Disposables;
//using System.Reactive.Linq;
//using System.Reactive.Threading.Tasks;
//using Microsoft.JSInterop;
//using Shiny.Web.Infrastructure;

//namespace Shiny.BluetoothLE;


//public class BleManager : IBleManager
//{
//    readonly IJSRuntime js;


//    public BleManager(IJSRuntime js)
//    {
//        this.js = js;
//    }


//    IJSObjectReference? jsRef;
//    async Task<IJSObjectReference> GetModule()
//    {
//        this.jsRef ??= await this.js.Import("Shiny.BluetoothLE.Blazor", "manager");
//        return this.jsRef;
//    }

//    //public async ValueTask DisposeAsync()
//    //{
//    //    if (moduleTask.IsValueCreated)
//    //    {
//    //        var module = await moduleTask.Value;
//    //        await module.DisposeAsync();
//    //    }
//    //}
//    public bool IsScanning { get; private set; }


//    public IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string serviceUuid = null)
//    {
//        throw new NotImplementedException();
//    }


//    public IObservable<IPeripheral> GetKnownPeripheral(string peripheralUuid)
//    {
//        throw new NotImplementedException();
//    }



//    public IObservable<ScanResult> Scan(ScanConfig? config = null)
//    {
//        if (this.IsScanning)
//            throw new ArgumentException("There is already a scan in progress");

//        return this
//            .GetModule()
//            .ToObservable()
//            .Select(mod => Observable.Create<ScanResult>(async ob =>
//            {
//                var comp = new CompositeDisposable();
//                var interop = JsCallback<JsScanResult>.CreateInterop().DisposedBy(comp);
//                interop
//                    .Value
//                    .WhenResult()
//                    .Subscribe(jsScan =>
//                    {
//                        // TODO
//                        var result = new ScanResult(null, jsScan.Rssi, null);
//                        ob.OnNext(result);
//                    })
//                    .DisposedBy(comp);

//                await mod.InvokeVoidAsync("shinyBle.startScan", interop); // TODO: pass args
//                this.IsScanning = true;

//                return async () =>
//                {
//                    await this.js.InvokeVoidAsync("shinyBle.stopScan");
//                    comp.Dispose();
//                };
//            }))
//            .Switch();
//    }


//    public async void StopScan()
//    {
//        if (!this.IsScanning)
//            return;

//        this.IsScanning = true;
//        await this.js.InvokeVoidAsync("shinyBleManager.stopScan");
//    }


//    //public IObservable<AccessState> WhenStatusChanged() => this
//    //    .GetModule()
//    //    .ToObservable()
//    //    .Select(mod => Observable.Create<AccessState>(ob =>
//    //    {
//    //        var watch = JsCallback<string>.CreateInterop();
//    //        var sub = watch.Value.WhenResult().Select(Utils.ToAccessState).Subscribe(state => ob.OnNext(state));

//    //        mod.InvokeVoidAsync("shinyBle.whenStatusChanged", watch);
//    //        return () =>
//    //        {
//    //            watch?.Dispose();
//    //            sub?.Dispose();
//    //        };
//    //    }))
//    //    .Switch();
//    public IObservable<AccessState> RequestAccess(bool connect = true) => throw new NotImplementedException();
//}

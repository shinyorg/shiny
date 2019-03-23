using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.JSInterop;


namespace Acr.BluetoothLE.Central
{
    public class CentralManager : AbstractCentralManager
    {
        public override IObservable<AccessState> RequestAccess() => Observable.FromAsync(async () =>
        {
            var result = await JSRuntime.Current.InvokeAsync<bool>("AcrBle.isSupported");
            var r = result ? AccessState.Available : AccessState.Denied;
            return r;
        });


        public override IObservable<IScanResult> Scan(ScanConfig config = null)
        {
            if (!config.ServiceUuids.Any())
                throw new ArgumentException("You must provide a serviceUUID on this platform to scan");


            return Observable.Create<IScanResult>(ob =>
            {
                // TODO: hook to callbacks
                JSRuntime.Current.InvokeAsync<object>("AcrBle.scan");
                return () => { };
            });
        }


        public override IObservable<AccessState> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }


        public override void StopScan()
            => JSRuntime.Current.InvokeAsync<object>("AcrBle.stopScan");
    }
}

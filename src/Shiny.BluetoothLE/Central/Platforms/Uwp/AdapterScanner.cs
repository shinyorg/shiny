//using System;
//using System.Reactive.Disposables;
//using System.Reactive.Linq;
//using Windows.Devices.Bluetooth;
//using Windows.Devices.Enumeration;
//using Shiny.Logging;


//namespace Acr.BluetoothLE.Peripherals
//{
//    public class AdapterScanner : IAdapterScanner
//    {
//        public bool IsSupported => true;


//        public IObservable<IAdapter> FindAdapters() => Observable.Create<IAdapter>(async ob =>
//        {
//            var peripherals = await DeviceInformation.FindAllAsync(BluetoothAdapter.GetDeviceSelector());
//            foreach (var dev in peripherals)
//            {
//                Log.Info(BleLogCategory.Adapter, "found - {dev.Name} ({dev.Kind} - {dev.Id})");

//                var native = await BluetoothAdapter.FromIdAsync(dev.Id);
//                if (native.IsLowEnergySupported)
//                {
//                    var radio = await native.GetRadioAsync();
//                    var adapter = new Adapter(native, radio);
//                    ob.OnNext(adapter);
//                }
//            }
//            ob.OnCompleted();
//            return Disposable.Empty;
//        });
//    }
//}
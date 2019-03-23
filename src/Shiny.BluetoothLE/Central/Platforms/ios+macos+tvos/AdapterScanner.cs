//#if __MACOS__
//using System;
//using System.Reactive.Linq;


//namespace Plugin.BluetoothLE
//{
//    public class AdapterScanner : IAdapterScanner
//    {
//        public bool IsSupported => false;
//        public IObservable<IAdapter> FindAdapters() => Observable.Return(CrossBleAdapter.Current);
//    }
//}
//#endif
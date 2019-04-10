//using System;
//using Network;


//namespace Shiny.Net
//{
//    public class ConnectivityImpl : SharedConnectivityImpl
//    {
//        readonly NWPathMonitor monitor;


//        public ConnectivityImpl()
//        {
//            this.monitor = new NWPathMonitor();
//            this.monitor.SetUpdatedSnapshotHandler(path =>
//            {
//                //NWInterfaceType.Loopback
//                path.UsesInterfaceType(NWInterfaceType.Wifi);
//                path.UsesInterfaceType(NWInterfaceType.Wired);
//                path.UsesInterfaceType(NWInterfaceType.Cellular);

//                //path.Status == NWPathStatus.Satisfied
//                //path.EffectiveRemoteEndpoint.
//                //path.EffectiveLocalEndpoint.
//            });
//            this.monitor.SetMonitorCanceledHandler(() =>
//            {

//            });
//        }


//        protected override void OnNpcHookChanged(bool hasSubscribers)
//        {
//            if (hasSubscribers)
//            {
//                this.monitor.Start();
//            }
//            else
//            {
//                this.monitor.Cancel();
//            }
//        }
//    }
//}

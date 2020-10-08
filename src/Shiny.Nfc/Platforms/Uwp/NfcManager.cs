using System;
using System.Reactive.Linq;

using Windows.Networking.Proximity;

namespace Shiny.Nfc
{
    public class NfcManager : INfcManager
    {
        readonly ProximityDevice device;
        public NfcManager() => this.device = ProximityDevice.GetDefault();


        public IObservable<AccessState> RequestAccess()
        {
            var status = this.device == null ? AccessState.NotSupported : AccessState.Available;
            return Observable.Return(status);
        }


        public IObservable<NDefRecord[]> SingleRead() => this.ContinuousRead().Take(1);


        public IObservable<NDefRecord[]> ContinuousRead() => Observable.Create<NDefRecord[]>(ob =>
        {
            var sub = this.device.SubscribeForMessage("TODO", (dev, msg) =>
            {
                //msg.MessageType
                //msg.Data // IBuffer
            });
            return () => this.device.StopSubscribingForMessage(sub);
        });

        private void Device_DeviceArrived(ProximityDevice sender)
        {
            throw new NotImplementedException();
        }
    }
}

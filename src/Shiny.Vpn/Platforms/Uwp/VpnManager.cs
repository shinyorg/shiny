using System;
using System.Reactive;
using System.Threading.Tasks;


namespace Shiny.Vpn
{
    public class VpnManager : IVpnManager
    {
        public VpnConnectionState Status => throw new NotImplementedException();

        public IObservable<Unit> Connect(VpnConnectionOptions opts)
        {
            throw new NotImplementedException();
        }

        public Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public IObservable<VpnConnectionState> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Threading.Tasks;

namespace Shiny.Vpn
{
    public class VpnManager : IVpnManager
    {
        public VpnConnectionState Status => throw new NotImplementedException();

        public Task Connect(VpnConnectionOptions opts)
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

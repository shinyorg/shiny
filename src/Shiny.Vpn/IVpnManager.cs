using System;
using System.Threading.Tasks;


namespace Shiny.Vpn

{
    public interface IVpnManager
    {
        Task Connect(VpnConnectionOptions opts);
        Task Disconnect();

        VpnConnectionState Status { get; }
        IObservable<VpnConnectionState> WhenStatusChanged();
    }
}

using System;
using System.Reactive;
using System.Threading.Tasks;


namespace Shiny.Vpn

{
    public interface IVpnManager
    {
        IObservable<Unit> Connect(VpnConnectionOptions opts);
        Task Disconnect();

        VpnConnectionState Status { get; }
        IObservable<VpnConnectionState> WhenStatusChanged();
    }
}

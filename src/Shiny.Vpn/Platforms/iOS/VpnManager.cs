using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Foundation;
using NetworkExtension;


namespace Shiny.Vpn
{
    public class VpnManager : IVpnManager
    {
        public VpnConnectionState Status
        {
            get
            {
                var c = NEVpnManager.SharedManager.Connection;
                if (c == null)
                    return VpnConnectionState.Disconnected;

                var status = Enum.Parse<VpnConnectionState>(c.Status.ToString(), true);
                return status;
            }
        }


        public Task Connect(VpnConnectionOptions opts)
        {
            var c = NEVpnManager.SharedManager.Connection;
            if (!c.StartVpnTunnel(null, out NSError e)) 
                throw new ArgumentException("Unable to start VPN - " + e.LocalizedDescription);
            

            //NEVpnManager.SharedManager.Connection.StartVpnTunnel
            //NEVpnManager.SharedManager.Connection.StopVpnTunnel()
            //NEVpnManager.SharedManager.Connection.Status == NEVpnStatus.Connected
            //NEVpnManager.SharedManager.Enabled
            //NEVpnManager.SharedManager.SaveToPreferencesAsync
            //NEVpnManager.SharedManager.RemoveFromPreferencesAsync
            return Task.CompletedTask;
        }


        public Task Disconnect()
        {
            return Task.CompletedTask;
        }


        public IObservable<VpnConnectionState> WhenStatusChanged() => Observable
            .Create<VpnConnectionState>(ob =>
            {
                ob.OnNext(this.Status); // broadcast current status
                var sub = NEVpnManager.Notifications.ObserveConfigurationChange((sender, args) =>
                    ob.OnNext(this.Status)
                );
                return () => sub.Dispose();
            });
    }
}

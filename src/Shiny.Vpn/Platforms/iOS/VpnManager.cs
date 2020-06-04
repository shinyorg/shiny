using System;
using System.Reactive;
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


        public IObservable<Unit> Connect(VpnConnectionOptions opts) => Observable.Create<Unit>(ob => 
        {
            //NEVpnManager.SharedManager.LoadFromPreferences(_ => { })
            this.SetProtocol(opts);
            this.StartConnection(opts);
            //NEVpnManager.SharedManager.Enabled
            //NEVpnManager.SharedManager.SaveToPreferencesAsync
            //NEVpnManager.SharedManager.RemoveFromPreferencesAsync
            return () => { };
        });


        public Task Disconnect()
        {
            NEVpnManager.SharedManager.Connection.StopVpnTunnel();
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
            })
            .DistinctUntilChanged();


        protected virtual void SetProtocol(VpnConnectionOptions opts)
        {
            var mgr = NEVpnManager.SharedManager;
            mgr.Protocol = new NEVpnProtocolIpSec
            {
                //AuthenticationMethod = NEVpnIp.Certificate, // Certificate, None, SharedSecret
                LocalIdentifier = "shiny",
                RemoteIdentifier = "shiny",
                //PasswordReference = ""
                ServerAddress = opts.ServerAddress,
                SharedSecretReference = NSData.FromString("")
            };
            mgr.Protocol = new NEVpnProtocolIke2
            {
                //CertificateType = NEVpnIke2CertificateType.ECDSA256,
                //ServerCertificateCommonName = "",
                //ServerCertificateIssuerCommonName = "",
                //PasswordReference
                AuthenticationMethod = NEVpnIkeAuthenticationMethod.None, // Certificate, None, SharedSecret
                LocalIdentifier = "shiny",
                RemoteIdentifier = "shiny",
                ServerAddress = opts.ServerAddress,
                SharedSecretReference = NSData.FromString("")
            };


            //var protocol = new NEVpnProtocolIke2
            //{
            //    UseExtendedAuthentication = true,
            //    LocalIdentifier = Constants.LocalId,
            //    RemoteIdentifier = Constants.RemoteId,
            //    ServerAddress = Constants.ServerAddress,
            //    PasswordReference = KeychainHelper.GetRecordsFromKeychain("VPN_Password", Security.SecKind.GenericPassword),
            //    Username = new NSString("fbvkbqlp"),
            //};
        }


        protected virtual void StartConnection(VpnConnectionOptions opts)
        {
            var native = new NEVpnConnectionStartOptions();
            var c = NEVpnManager.SharedManager.Connection;

            native.Username = new NSString(opts.UserName);
            native.Password = new NSString(opts.Password);

            if (!c.StartVpnTunnel(native, out var e))
                throw new ArgumentException("Unable to start VPN - " + e.LocalizedDescription);
        }
    }
}

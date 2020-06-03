using System;
using System.Reactive;
using System.Threading.Tasks;
using Android.Net;


namespace Shiny.Vpn
{
    //<service android:name=".ExampleVpnService"
    //         android:permission="android.permission.BIND_VPN_SERVICE">
    //     <intent-filter>
    //         <action android:name="android.net.VpnService"/>
    //     </intent-filter>
    // </service>

    //<service android:name=".ExampleVpnService"
    //        android:permission="android.permission.BIND_VPN_SERVICE">
    //    <intent-filter>
    //        <action android:name="android.net.VpnService"/>
    //    </intent-filter>
    //    <meta-data android:name="android.net.VpnService.SUPPORTS_ALWAYS_ON"
    //            android:value=false/>
    //</service>

    //https://android.googlesource.com/platform/development/+/master/samples/ToyVpn/src/com/example/android/toyvpn
    //https://developer.android.com/guide/topics/connectivity/vpn
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

using System;

using Android.App;
using Android.Content;
using Android.Net;
using Android.Runtime;

namespace Shiny.Vpn.Platforms.Android
{

    public class ShinyVpnService : VpnService
    {

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            var address = intent.GetStringExtra("Address");

            var localTunnel = new Builder(this)
                .AddAddress("192.168.2.2", 24)
                .AddRoute("0.0.0.0", 0) // you must have at least one route
            //    .AddDnsServer("192.168.1.1")
                .Establish();
            return base.OnStartCommand(intent, flags, startId);
        }
    }
}
using System;
using Shiny.Net;
using Xamarin.Essentials;
using NetAccess = Shiny.Net.NetworkAccess;
using XamAccess = Xamarin.Essentials.NetworkAccess;


namespace Shiny.Integrations.XamEssentials
{
    public class ConnectivityImpl : SharedConnectivityImpl
    {
        public override NetworkReach Reach => Connectivity.NetworkAccess switch
        {
            XamAccess.ConstrainedInternet => NetworkReach.ConstrainedInternet
        };
        public override NetAccess Access => Connectivity.NetworkAccess switch
        {
            XamAccess.Internet => NetAccess.Ethernet
        };


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            base.OnNpcHookChanged(hasSubscribers);
            if (hasSubscribers)
            {
                Connectivity.ConnectivityChanged += this.OnConnectivityChanged;
            }
            else
            {
                Connectivity.ConnectivityChanged -= this.OnConnectivityChanged;
            }
        }


        void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
            => this.RaisePropertyChanged();
    }
}

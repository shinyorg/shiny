using System;
using System.ComponentModel;
using Shiny.Net;
using Xamarin.Essentials;


namespace Shiny.Integrations.XamEssentials
{
    public class EssentialsConnectivityImpl : Shiny.Net.SharedConnectivityImpl
    {
        public NetworkReach Reach
        {
            get
            {
                switch (Connectivity.NetworkAccess)
                {
                    case Xamarin.Essentials.NetworkAccess.ConstrainedInternet:
                        return Shiny.Net.NetworkReach.ConstrainedInternet;

                    case Xamarin.Essentials.NetworkAccess.Internet:
                        return Shiny.Net.NetworkReach.Internet;

                    case Xamarin.Essentials.NetworkAccess.Local:
                        return Shiny.Net.NetworkReach.Local;

                    case Xamarin.Essentials.NetworkAccess.None:
                        return Shiny.Net.NetworkReach.None;

                    case Xamarin.Essentials.NetworkAccess.Unknown:
                    default:
                        return Shiny.Net.NetworkReach.Unknown;
                }
            }
        }

        public Shiny.Net.NetworkAccess Access
        {
            get
            {
                return Shiny.Net.NetworkAccess.Unknown;
            }
        }


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            base.OnNpcHookChanged(hasSubscribers);
            if (hasSubscribers)
            {
                Connectivity.ConnectivityChanged += null;
            }
            else
            {
                Connectivity.ConnectivityChanged -= null;
            }
        }
    }
}

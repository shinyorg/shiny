using System;
using System.Collections.Generic;
using Tizen.Network.Connection;


namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        public NetworkReach Reach => throw new NotImplementedException();

        public NetworkAccess Access => throw new NotImplementedException();

        public string? CellularCarrier => throw new NotImplementedException();

        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            //    Permissions.EnsureDeclared(PermissionType.NetworkState);
            //if (hasSubscribers)
            //    ConnectionManager.ConnectionTypeChanged += null;
            //else
            //    ConnectionManager.ReachabilityChanged -= null;
        }


        //    var list = await ConnectionProfileManager.GetProfileListAsync(ProfileListType.Connected);
        //    profiles.Clear();
        //    foreach (var result in list)
        //    {
        //        switch (result.Type)
        //        {
        //            case ConnectionProfileType.Bt:
        //                profiles.Add(ConnectionProfile.Bluetooth);
        //                break;

        //            case ConnectionProfileType.Cellular:
        //                profiles.Add(ConnectionProfile.Cellular);
        //                break;

        //            case ConnectionProfileType.Ethernet:
        //                profiles.Add(ConnectionProfile.Ethernet);
        //                break;

        //            case ConnectionProfileType.WiFi:
        //                profiles.Add(ConnectionProfile.WiFi);
        //                break;
        //        }
        //    }
        //    OnConnectivityChanged();


        //static NetworkAccess PlatformNetworkAccess
        //{
        //    get
        //    {
        //        Permissions.EnsureDeclared(PermissionType.NetworkState);
        //        var currentAccess = ConnectionManager.CurrentConnection;
        //        switch (currentAccess.Type)
        //        {
        //            case ConnectionType.WiFi:
        //            case ConnectionType.Cellular:
        //            case ConnectionType.Ethernet:
        //                return NetworkAccess.Internet;
        //            default:
        //                return NetworkAccess.None;
        //        }
        //    }
        //}
    }
}
using System;
using System.Reactive.Linq;
using Network;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity
{
    readonly NWPathMonitor netmon = new();


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            if (this.netmon.CurrentPath == null)
                return ConnectionTypes.None;

            ConnectionTypes types = 0;
            this.netmon.CurrentPath.EnumerateInterfaces(nw =>
            {
                var type = nw.InterfaceType switch
                {
                    NWInterfaceType.Wifi => ConnectionTypes.Wifi,
                    NWInterfaceType.Wired => ConnectionTypes.Wired,
                    NWInterfaceType.Cellular => ConnectionTypes.Cellular,
                    _ => ConnectionTypes.Unknown // loopback & other
                };
                types |= type;
            });
            return types;
        }
    }


    public NetworkAccess Access
    {
        get
        {
            var monitor = new NWPathMonitor();
            if (monitor.CurrentPath == null)
                return NetworkAccess.None;

            var types = this.ConnectionTypes;
            if (types.HasFlag(ConnectionTypes.Wifi) || types.HasFlag(ConnectionTypes.Wired))
                return NetworkAccess.Internet;

            var restricted = monitor.CurrentPath.Status != NWPathStatus.Satisfiable;
            if (!restricted && types.HasFlag(ConnectionTypes.Cellular))
            {
                if (monitor.CurrentPath.IsConstrained)
                    return NetworkAccess.ConstrainedInternet;

                return NetworkAccess.Internet;
            }

            return NetworkAccess.None;
        }
    }


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        this.netmon.SnapshotHandler = _ => ob.OnNext(this);
        this.netmon.Start();

        return () =>
        {
            this.netmon.Cancel();
            this.netmon.SnapshotHandler = null;
        };
    });
}

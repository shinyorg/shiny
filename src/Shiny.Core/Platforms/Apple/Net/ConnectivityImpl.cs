using System;
using System.Reactive.Linq;
using Network;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity
{
    readonly NWPathMonitor netmon = new();
    bool started;


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            if (!this.started) this.netmon.Start();

            if (this.netmon.CurrentPath == null)
                return ConnectionTypes.None;

            ConnectionTypes types = 0;
            //this.netmon.CurrentPath.UsesInterfaceType(NWInterfaceType.Wifi);
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
            if (!this.started)
                this.netmon.Cancel();

            return types;
        }
    }


    public NetworkAccess Access
    {
        get
        {
            if (!this.started) this.netmon.Start();

            var monitor = new NWPathMonitor();
            if (monitor.CurrentPath == null)
                return NetworkAccess.None;

            var types = this.ConnectionTypes;
            if (types.HasFlag(ConnectionTypes.Wifi) || types.HasFlag(ConnectionTypes.Wired))
                return NetworkAccess.Internet; // should be satisfied as well

            var restricted = monitor.CurrentPath.Status != NWPathStatus.Satisfiable;
            if (!restricted && types.HasFlag(ConnectionTypes.Cellular))
            {
                if (monitor.CurrentPath.IsConstrained)
                    return NetworkAccess.ConstrainedInternet;

                return NetworkAccess.Internet;
            }
            if (!this.started)
                this.netmon.Cancel();

            return NetworkAccess.None;
        }
    }


    IObservable<IConnectivity>? connObs;
    public IObservable<IConnectivity> WhenChanged()
    {
        this.connObs ??= Observable
            .Create<IConnectivity>(ob =>
            {
                var s = this.started;
                this.netmon.SnapshotHandler = _ => ob.OnNext(this);
                if (!s)
                    this.netmon.Start();

                return () =>
                {
                    this.started = false;
                    this.netmon.Cancel();
                    this.netmon.SnapshotHandler = null;
                };
            })
            .Publish()
            .RefCount();

        return this.connObs;
    }
}
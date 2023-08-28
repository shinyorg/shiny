using System;
using System.Reactive.Linq;
using CoreFoundation;
using Microsoft.Extensions.Logging;
using Network;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity, IDisposable
{
    readonly NWPathMonitor netmon;
    readonly ILogger logger;


    public ConnectivityImpl(ILogger<ConnectivityImpl> logger)
    {
        this.logger = logger;
        this.netmon = new();
        this.netmon.SetQueue(DispatchQueue.DefaultGlobalQueue);
        this.netmon.Start();
    }

    
    public IObservable<IConnectivity> WhenChanged() => Observable
        .Create<IConnectivity>(ob =>
        {
            var action = (NWPath path) =>
            {
                this.logger.NetworkChange(this.ConnectionTypes, this.Access);
                ob.OnNext(this);
            };
            this.netmon.SnapshotHandler += action;
            //this.netmon.Start();
            
            return () =>
            {
                //this.netmon.Cancel();
                this.netmon.SnapshotHandler -= action;
            };
        })
        .StartWith(this)
        .Publish()
        .RefCount();
    

    public ConnectionTypes ConnectionTypes
    {
        get
        {
            var path = this.netmon.CurrentPath;
            
            if (path == null || path.Status != NWPathStatus.Satisfied)
                return ConnectionTypes.None;
            
            ConnectionTypes types = 0;
            if (path.UsesInterfaceType(NWInterfaceType.Wifi))
                types |= ConnectionTypes.Wifi;
            
            if (path.UsesInterfaceType(NWInterfaceType.Wired))
                types |= ConnectionTypes.Wired;

            if (path.UsesInterfaceType(NWInterfaceType.Cellular))
                types |= ConnectionTypes.Cellular;            
            
            return types;
        }
    }

    
    public NetworkAccess Access
    {
        get
        {
            var access = NetworkAccess.None;
            var p = this.netmon.CurrentPath;
            
            if (p?.Status == NWPathStatus.Satisfied)
                access = p.IsConstrained ? NetworkAccess.ConstrainedInternet : NetworkAccess.Internet;

            return access;
        }
    }


    public void Dispose()
    {
        this.netmon.Cancel();
        this.netmon.Dispose();
    }
}

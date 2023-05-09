using System;
using System.Reactive.Linq;
using CoreFoundation;
using Microsoft.Extensions.Logging;
using Network;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity
{
    readonly NWPathMonitor netmon = new();
    readonly ILogger logger;

    public ConnectivityImpl(ILogger<ConnectivityImpl> logger)
    {
        this.logger = logger;
    }

    
    public IObservable<IConnectivity> WhenChanged() => Observable
        .Create<IConnectivity>(ob =>
        {
            var action = (NWPath path) =>
            {
                if (this.logger.IsEnabled(LogLevel.Debug))
                    this.logger.LogDebug($"Network change detected - {this.ConnectionTypes} - {this.Access}");
                ob.OnNext(this);
            };
            this.netmon.SnapshotHandler += action;
            this.netmon.SetQueue(DispatchQueue.DefaultGlobalQueue);
            this.netmon.Start();
            
            return () =>
            {
                this.netmon.Cancel();
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
}

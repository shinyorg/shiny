using System;
using CoreFoundation;
using Microsoft.Extensions.Logging;
using Network;
using Shiny.Net;

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
        this.netmon.SnapshotHandler += this.OnPathChanged;
        this.netmon.Start();
    }


    public event EventHandler? Changed;


    void OnPathChanged(NWPath path)
    {
        this.logger.NetworkChange(this.ConnectionTypes, this.Access);
        this.Changed?.Invoke(this, EventArgs.Empty);
    }


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
        this.netmon.SnapshotHandler -= this.OnPathChanged;
        this.netmon.Cancel();
        this.netmon.Dispose();
    }
}

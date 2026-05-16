using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Net;

namespace Shiny.Infrastructure;


public class ConnectivityManager : IConnectivity, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    IJSObjectReference? module;
    DotNetObjectReference<ConnectivityManager>? objRef;


    public ConnectivityManager(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await this.jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Support.DeviceMonitoring.Blazor/connectivity.js")
            .ConfigureAwait(false);

        return this.module;
    }


    public event EventHandler? Changed;


    [JSInvokable]
    public void OnChange() => this.Changed?.Invoke(this, EventArgs.Empty);


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            if (this.module == null)
                return ConnectionTypes.Unknown;

            var type = ((IJSInProcessObjectReference)this.module).Invoke<string>("getConnType");
            return type switch
            {
                "bluetooth" => ConnectionTypes.Bluetooth,
                "ethernet" => ConnectionTypes.Wired,
                "cellular" => ConnectionTypes.Cellular,
                "wifi" => ConnectionTypes.Wifi,
                "wimax" => ConnectionTypes.Wifi,
                "none" => ConnectionTypes.None,
                _ => ConnectionTypes.Unknown
            };
        }
    }


    public NetworkAccess Access
    {
        get
        {
            if (this.module == null)
                return NetworkAccess.Unknown;

            var online = ((IJSInProcessObjectReference)this.module).Invoke<bool>("isConnected");
            return online ? NetworkAccess.Internet : NetworkAccess.None;
        }
    }


    public async Task StartAsync()
    {
        var mod = await this.GetModule().ConfigureAwait(false);
        this.objRef = DotNetObjectReference.Create(this);
        await mod.InvokeVoidAsync("startListener", this.objRef).ConfigureAwait(false);
    }


    public async ValueTask DisposeAsync()
    {
        if (this.module != null)
        {
            try
            {
                await this.module.InvokeVoidAsync("stopListener").ConfigureAwait(false);
            }
            catch { }
        }
        this.objRef?.Dispose();
        this.objRef = null;
        if (this.module != null)
        {
            await this.module.DisposeAsync().ConfigureAwait(false);
            this.module = null;
        }
    }
}

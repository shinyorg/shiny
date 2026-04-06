using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Net;

namespace Shiny.Infrastructure;


public class ConnectivityManager : IConnectivity, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    readonly Subject<Unit> connSubj = new();
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


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var sub = this.connSubj.Subscribe(_ => ob.OnNext(this));

        _ = Task.Run(async () =>
        {
            try
            {
                var mod = await this.GetModule().ConfigureAwait(false);
                this.objRef = DotNetObjectReference.Create(this);
                await mod.InvokeVoidAsync("startListener", this.objRef).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ob.OnError(ex);
            }
        });

        return () =>
        {
            sub.Dispose();
            _ = Task.Run(async () =>
            {
                try
                {
                    var mod = await this.GetModule().ConfigureAwait(false);
                    await mod.InvokeVoidAsync("stopListener").ConfigureAwait(false);
                    this.objRef?.Dispose();
                    this.objRef = null;
                }
                catch { }
            });
        };
    });


    [JSInvokable]
    public void OnChange() => this.connSubj.OnNext(Unit.Default);


    public async ValueTask DisposeAsync()
    {
        this.objRef?.Dispose();
        if (this.module != null)
        {
            await this.module.DisposeAsync().ConfigureAwait(false);
            this.module = null;
        }
    }
}

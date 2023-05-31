using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Net;


public class Connectivity : IConnectivity
{
    readonly Subject<Unit> connSubj = new();
    //IJSInProcessObjectReference? module;


    //public async Task OnStart(IJSInProcessRuntime jsRuntime)
    //{
    //    this.module = await jsRuntime.ImportInProcess("Shiny.Core.Blazor", "connectivity.js");
    //    this.module.InvokeVoid("init");
    //}


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            //var type = (this.module?.Invoke<string>("getConnType") ?? "Unknown");
            var type = "Unknown";
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
            if (this.ConnectionTypes == ConnectionTypes.None)
                return NetworkAccess.None;

            //'slow-2g', '2g', '3g', or '4g'
            //var state = this.module?.Invoke<string>("getEffectiveType") ?? "Unknown";
            //var online = this.module?.Invoke<bool>("isConnected") ?? false;
            //return online ? NetworkAccess.Internet : NetworkAccess.None;
            return NetworkAccess.None;
        }
    }


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var sub = this.connSubj.Subscribe(_ => ob.OnNext(this));
        //var objRef = DotNetObjectReference.Create(this);
        //this.module!.InvokeVoid("startListener", objRef);

        return () =>
        {
            //this.module!.InvokeVoid("stopListener");
            //objRef?.Dispose();
            sub?.Dispose();
        };
    });
}
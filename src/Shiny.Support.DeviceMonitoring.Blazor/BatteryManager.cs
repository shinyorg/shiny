using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Power;

namespace Shiny.Infrastructure;


public class BatteryManager : IBattery, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    readonly Subject<Unit> changeSubj = new();
    IJSObjectReference? module;
    DotNetObjectReference<BatteryManager>? objRef;


    public BatteryManager(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await this.jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Support.DeviceMonitoring.Blazor/battery.js")
            .ConfigureAwait(false);

        return this.module;
    }


    public BatteryState Status
    {
        get
        {
            if (this.module == null)
                return BatteryState.Unknown;

            var charging = ((IJSInProcessObjectReference)this.module).Invoke<bool>("isCharging");
            return charging ? BatteryState.Charging : BatteryState.Discharging;
        }
    }


    public double Level
    {
        get
        {
            if (this.module == null)
                return 1.0;

            return ((IJSInProcessObjectReference)this.module).Invoke<double>("getLevel");
        }
    }


    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        var sub = this.changeSubj.Subscribe(_ => ob.OnNext(this));

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
    public void OnChange() => this.changeSubj.OnNext(Unit.Default);


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

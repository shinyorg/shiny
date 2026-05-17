using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Power;

namespace Shiny.Infrastructure;


public class BatteryManager : IBattery, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    IJSObjectReference? module;
    DotNetObjectReference<BatteryManager>? objRef;


    public BatteryManager(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await this.jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Core.Blazor/battery.js")
            .ConfigureAwait(false);

        return this.module;
    }


    public event EventHandler? Changed;


    [JSInvokable]
    public void OnChange() => this.Changed?.Invoke(this, EventArgs.Empty);


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

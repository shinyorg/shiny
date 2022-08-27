using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.JSInterop;
using Shiny.Power;

namespace Shiny.Infrastructure;


public class Battery : IBattery, IShinyStartupTask, IDisposable
{
    readonly Subject<Unit> changeSubj = new();
    readonly IJSRuntime jsRuntime;
    IJSInProcessObjectReference module = null!;


    public Battery(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }


    public async void Start()
    {
        this.module = await this.jsRuntime.ImportInProcess("Shiny.Core.Blazor", "battery.js");
        this.module.InvokeVoid("init");
    }


    public BatteryState Status => this.module?.Invoke<bool>("isCharging") ?? false ? BatteryState.Charging : BatteryState.Discharging;
    public double Level => this.module?.Invoke<double>("getLevel") ?? 0.0;
    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(async ob =>
    {
        var sub = this.changeSubj.Subscribe(_ => ob.OnNext(this));
        var objRef = DotNetObjectReference.Create(this);
        this.module.InvokeVoid("startListener", objRef);

        return () =>
        {
            this.module.InvokeVoid("stopListener");
            objRef?.Dispose();
            sub?.Dispose();
        };
    });


    [JSInvokable] public void OnChange() => this.changeSubj.OnNext(Unit.Default);
    public void Dispose() => this.module?.Dispose();
}


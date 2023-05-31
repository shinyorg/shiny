using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Power;

namespace Shiny.Infrastructure;


public class Battery : IBattery
{
    readonly Subject<Unit> changeSubj = new();
    //IJSInProcessObjectReference module = null!;


    //public async Task OnStart(IJSInProcessRuntime jsRuntime)
    //{
    //    this.module = await jsRuntime.ImportInProcess("Shiny.Core.Blazor", "battery.js");
    //    this.module.InvokeVoid("init");
    //}


    public BatteryState Status => Interop.IsBatteryCharging() ? BatteryState.Charging : BatteryState.Discharging;
    public double Level => Interop.GetBatteryLevel();
    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        var sub = this.changeSubj.Subscribe(_ => ob.OnNext(this));
        //var objRef = DotNetObjectReference.Create(this);
        //this.module.InvokeVoid("startListener", objRef);

        return () =>
        {
            //this.module.InvokeVoid("stopListener");
            //objRef?.Dispose();
            sub?.Dispose();
        };
    });


    //[JSInvokable] public void OnChange() => this.changeSubj.OnNext(Unit.Default);
    //public void Dispose() => this.module?.Dispose();
}


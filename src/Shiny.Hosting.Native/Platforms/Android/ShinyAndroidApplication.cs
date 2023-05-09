using System;
using Android.App;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyAndroidApplication : Application
{
    protected abstract IHost CreateShinyHost();


    public override void OnCreate()
    {
        base.OnCreate();
        var host = this.CreateShinyHost();
        host.Run();
    }
}
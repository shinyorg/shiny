using System;
using Android.App;
using Android.Runtime;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyAndroidApplication : Application
{
    protected ShinyAndroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}
    
    
    protected abstract IHost CreateShinyHost();


    public override void OnCreate()
    {
        base.OnCreate();
        var host = this.CreateShinyHost();
        host.Run();
    }
}
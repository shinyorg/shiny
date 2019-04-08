using System;
using Shiny;
using Android.App;
using Android.Runtime;
using Samples.ShinySetup;


namespace Samples.Droid
{
    [Application]
    public class MainApplication : Application
    {
        public MainApplication() : base() { }
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }


        public override void OnCreate()
        {
            base.OnCreate();
            AndroidShinyHost.Init(this, new SampleStartup());
        }
    }
}
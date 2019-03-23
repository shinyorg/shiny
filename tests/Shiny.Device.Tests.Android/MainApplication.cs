using System;
using Android.App;
using Android.Runtime;


namespace Shiny.Device.Tests.Droid
{
    [Application]
    public class MainApplication : ShinyAndroidApplication<TestStartup>
    {
        public MainApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
    }
}

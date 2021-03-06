using System;
using Android.App;
using Android.Runtime;

namespace Shiny.Device.Tests.Droid
{
    [Application]
    public class MainApplication : Application
    {
        protected MainApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }


        public override void OnCreate()
        {
            base.OnCreate();
            this.ShinyOnCreate(new TestStartup());
        }
    }
}
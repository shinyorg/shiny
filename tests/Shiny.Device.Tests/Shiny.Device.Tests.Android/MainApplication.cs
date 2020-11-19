using System;
using Android.App;


namespace Shiny.Device.Tests.Droid
{
    [Application]
    public class MainApplication : Application
    {
        public override void OnCreate() => this.ShinyOnCreate(new TestStartup());
    }
}
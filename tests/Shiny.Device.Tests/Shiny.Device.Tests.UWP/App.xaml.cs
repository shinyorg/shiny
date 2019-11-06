using System;
using System.Reflection;
using Xunit.Runners.UI;


namespace Shiny.Device.Tests.Uwp
{
    sealed partial class App : RunnerApplication
    {
        protected override void OnInitializeRunner()
        {
            //Acr.Logging.Log.ToDebug();

            //this.AddTestAssembly(typeof(Plugin.BluetoothLE.Tests.DeviceTests).GetTypeInfo().Assembly);
            UwpShinyHost.Init(new TestStartup());
            this.AddTestAssembly(this.GetType().GetTypeInfo().Assembly);
        }
    }
}

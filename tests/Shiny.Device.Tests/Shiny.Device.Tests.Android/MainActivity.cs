using System;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Xunit.Runners.UI;


namespace Shiny.Device.Tests.Droid
{
    [Activity(
        Label = "Tests",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public partial class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            //Acr.Logging.Log.ToDebug();

            //UserDialogs.Init(() => this);
            //this.AddTestAssembly(typeof(BluetoothLE.Tests.DeviceTests).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            //CrossBleAdapter.UseNewScanner = false;
            //CrossBleAdapter.PauseBeforeServiceDiscovery = TimeSpan.FromSeconds(1);
            //CrossBleAdapter.PauseBetweenInvocations = TimeSpan.FromMilliseconds(250);
            //CrossBleAdapter.ShouldInvokeOnMainThread = false;

            this.AutoStart = false;
            this.TerminateAfterExecution = false;

            base.OnCreate(bundle);
        }
    }
}
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
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            //UserDialogs.Init(this);
            //Acr.Logging.Log.ToDebug();

            //this.RequestPermissions(new[]
            //{
            //    Manifest.Permission.AccessCoarseLocation,
            //    Manifest.Permission.BluetoothPrivileged
            //}, 0);

            //UserDialogs.Init(() => this);
            this.AddTestAssembly(typeof(TestStartup).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            //CrossBleAdapter.UseNewScanner = false;
            //CrossBleAdapter.PauseBeforeServiceDiscovery = TimeSpan.FromSeconds(1);
            //CrossBleAdapter.PauseBetweenInvocations = TimeSpan.FromMilliseconds(250);
            //CrossBleAdapter.ShouldInvokeOnMainThread = false;

            this.AutoStart = false;
            this.TerminateAfterExecution = false;

            base.OnCreate(bundle);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
            => AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Xunit.Runners.UI;

[assembly: Shiny.ShinyApplication(ShinyStartupTypeName = "Shiny.Tests.TestStartup")]


namespace Shiny.Tests.Droid
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
            this.ShinyOnCreate();
            this.AddTestAssembly(this.GetType().Assembly);
            this.AddTestAssembly(typeof(TestStartup).Assembly);

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
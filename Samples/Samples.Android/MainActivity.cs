using System;
using Shiny;
using Acr.UserDialogs;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Permission = Android.Content.PM.Permission;


namespace Samples.Droid
{
    [Activity(
        Label = "Shiny",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            UserDialogs.Init(() => (Activity)Forms.Context);

            base.OnCreate(savedInstanceState);
            Forms.Init(this, savedInstanceState);
            this.LoadApplication(new App());
        }


        //protected override void OnNewIntent(Android.Content.Intent intent)
        //{
        //    base.OnNewIntent(intent);
        //    Push.CheckLaunchedFromNotification(this, intent);
        //}


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
            => AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
using System;
using Shiny;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Runtime;
using Android.Support.V7.App;

namespace Samples.Droid
{
    [Activity(
        Label = "Shiny",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public partial class MainActivity : FormsAppCompatActivity
    {
        //protected override void OnCreate(Bundle savedInstanceState)
        //{
        //    base.OnCreate(savedInstanceState);
        //    Forms.Init(this, savedInstanceState);
        //    //FormsMaps.Init(this, savedInstanceState);

        //    //XF.Material.Droid.Material.Init(this, savedInstanceState);
        //    //this.LoadApplication(new App());

        //    this.ShinyOnCreate();
        //}


        //protected override void OnNewIntent(Intent intent)
        //{
        //    base.OnNewIntent(intent);
        //    this.ShinyOnNewIntent(intent);
        //}


        //public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        //{
        //    //    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        //    //    this.ShinyRequestPermissionsResult(requestCode, permissions, grantResults);
        //}
    }
}
using System;
using Shiny;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using Android.OS;
using Android.Runtime;

#if GENERATE_BOILERPLATE
[assembly: ShinyApplication(ShinyStartupTypeName = "Samples.SampleStartup")]
#endif

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
#if GENERATE_BOILERPLATE
#else
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            Forms.SetFlags(
                "SwipeView_Experimental",
                "Expander_Experimental",
                "RadioButton_Experimental"
            );
            Forms.Init(this, savedInstanceState);

            XF.Material.Droid.Material.Init(this, savedInstanceState);
            this.LoadApplication(new App());

            this.ShinyOnCreate();
        }


        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.ShinyOnNewIntent(intent);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
#endif
    }
}
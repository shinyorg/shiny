using System;
using Shiny;
using Android.App;
using Android.Content.PM;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using Android.OS;
using Android.Runtime;

[assembly: ShinyApplication(
    ShinyStartupTypeName = "Samples.SampleStartup",
    XamarinFormsAppTypeName = "Samples.App"
)]
[assembly: ShinyGeneratorDebug]


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
        partial void OnPreCreate(Bundle savedInstanceState) => Forms.SetFlags(
            "SwipeView_Experimental",
            "Expander_Experimental",
            "RadioButton_Experimental"
        );


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            // background denied, foreground & fine location = 0
            this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
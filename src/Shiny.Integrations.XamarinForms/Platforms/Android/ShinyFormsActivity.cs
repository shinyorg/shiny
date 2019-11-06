using System;
using Android.Content.PM;
using Android.Runtime;
using Xamarin.Forms.Platform.Android;


namespace Shiny
{
    public class ShinyFormsActivity : FormsAppCompatActivity
    {
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

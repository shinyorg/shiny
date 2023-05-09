using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyAndroidActivity : AppCompatActivity
{
    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        Host.Current.Lifecycle().OnActivityResult(this, requestCode, resultCode, data);
    }


    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Host.Current.Lifecycle().OnNewIntent(this, intent);
    }


    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        Host.Current.Lifecycle().OnRequestPermissionsResult(this, requestCode, permissions, grantResults);
    }
}
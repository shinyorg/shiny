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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Host.Lifecycle.OnActivityOnCreate(this, savedInstanceState);
    }


    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Host.Lifecycle.OnNewIntent(this, intent);
    }


    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        Host.Lifecycle.OnActivityResult(this, requestCode, resultCode, data);
    }


    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        Host.Lifecycle.OnRequestPermissionsResult(this, requestCode, permissions, grantResults);
    }
}
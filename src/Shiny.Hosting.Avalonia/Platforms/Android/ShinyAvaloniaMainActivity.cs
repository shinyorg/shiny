using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Avalonia.Android;
using Shiny.Hosting;

namespace Shiny;


/// <summary>
/// An <see cref="AvaloniaMainActivity"/> that forwards the activity events Shiny needs to
/// <see cref="Host.Lifecycle"/> - the Avalonia equivalent of what <c>UseShiny()</c> wires through MAUI's
/// lifecycle events. Without <see cref="OnRequestPermissionsResult"/> every Shiny permission request waits
/// forever, and without <see cref="OnCreate"/> / <see cref="OnNewIntent"/> notification taps never reach their delegate.
/// </summary>
/// <remarks>
/// If your activity can't derive from this, override the same four methods on yours and make the same calls.
/// </remarks>
public abstract class ShinyAvaloniaMainActivity : AvaloniaMainActivity
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

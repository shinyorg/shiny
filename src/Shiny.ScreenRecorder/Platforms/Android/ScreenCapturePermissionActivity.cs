using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media.Projection;
using Android.OS;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A transparent activity whose only job is to run the MediaProjection consent dialog and hand the
/// answer back.
/// </summary>
/// <remarks>
/// <para>MediaProjection consent cannot be requested any other way: the token that authorises a
/// capture *is* the activity result, and it is single-use. There is no permission to pre-grant and
/// no way to ask from a service.</para>
/// <para>Shiny.Core's <c>AndroidPlatform.Handle(activity, requestCode, resultCode, intent)</c> is
/// an empty stub, so rather than change Core this module carries its own one-shot activity. It is
/// declared <c>NoHistory</c> and <c>ExcludeFromRecents</c> so it never appears in the back stack or
/// the recents list - the user sees the system dialog over their own app and nothing else.</para>
/// </remarks>
[Activity(
    Enabled = true,
    Exported = false,
    NoHistory = true,
    ExcludeFromRecents = true,
    LaunchMode = LaunchMode.SingleTop,
    Theme = "@android:style/Theme.Translucent.NoTitleBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
)]
public class ScreenCapturePermissionActivity : Activity
{
    const int RequestCode = 0x5C21;

    // one consent flow at a time, matching the one-recording-at-a-time guarantee upstream
    static TaskCompletionSource<ScreenCaptureConsent>? pending;


    /// <summary>Launches the activity and waits for the user's answer.</summary>
    internal static Task<ScreenCaptureConsent> Request(AndroidPlatform platform, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<ScreenCaptureConsent>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (Interlocked.CompareExchange(ref pending, tcs, null) != null)
            throw new ScreenRecorderException("A screen capture consent prompt is already on screen");

        try
        {
            var intent = new Intent(platform.AppContext, typeof(ScreenCapturePermissionActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            platform.AppContext.StartActivity(intent);
        }
        catch
        {
            Interlocked.Exchange(ref pending, null);
            throw;
        }

        return tcs.Task.WaitAsync(ct);
    }


    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var manager = (MediaProjectionManager?)this.GetSystemService(MediaProjectionService);
        if (manager == null)
        {
            Complete(new ScreenCaptureConsent(false, 0, null));
            this.Finish();
            return;
        }

#pragma warning disable CA1422 // StartActivityForResult is soft-deprecated in favour of the AndroidX
        // ActivityResultLauncher, which needs a ComponentActivity and a registration made before
        // onStart. This activity exists for exactly one result and is destroyed immediately after,
        // so the older API is the correct shape here and the replacement would only add a
        // Fragment/AndroidX dependency to a package that otherwise needs none.
        this.StartActivityForResult(manager.CreateScreenCaptureIntent(), RequestCode);
#pragma warning restore CA1422
    }


#pragma warning disable CA1422 // paired with StartActivityForResult above
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
#pragma warning restore CA1422

        if (requestCode != RequestCode)
            return;

        Complete(new ScreenCaptureConsent(
            resultCode == Result.Ok && data != null,
            (int)resultCode,
            data
        ));

        this.Finish();
    }


    protected override void OnDestroy()
    {
        // covers the user dismissing the dialog with the back gesture, which delivers no result at
        // all - without this the caller would wait forever
        Complete(new ScreenCaptureConsent(false, (int)Result.Canceled, null));
        base.OnDestroy();
    }


    static void Complete(ScreenCaptureConsent consent)
        => Interlocked.Exchange(ref pending, null)?.TrySetResult(consent);
}


/// <summary>The result of the MediaProjection consent dialog.</summary>
/// <param name="Granted">Whether the user allowed the capture.</param>
/// <param name="ResultCode">The activity result code, which <c>getMediaProjection</c> needs back verbatim.</param>
/// <param name="Data">The consent token intent, which <c>getMediaProjection</c> also needs verbatim.</param>
internal record ScreenCaptureConsent(bool Granted, int ResultCode, Intent? Data);

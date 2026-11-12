using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Shiny.Biometrics.Infrastructure;

namespace Shiny.Biometrics;


/// <summary>
/// AndroidX BiometricPrompt, shown from the current <see cref="FragmentActivity"/>. Android does not say which sensor
/// a match came from, so <see cref="BiometricAvailability.Types"/> is the set of sensors the device declares.
/// </summary>
public class AndroidBiometrics(AndroidPlatform platform) : IBiometrics
{
    public Task<BiometricAvailability> GetAvailability(BiometricStrength strength = BiometricStrength.Strong, CancellationToken cancellationToken = default)
    {
        var context = platform.AppContext;
        var authenticators = BiometricMapper.ToAndroidAuthenticators(strength, false, (int)Build.VERSION.SdkInt);
        var code = BiometricManager.From(context).CanAuthenticate(authenticators);

        var keyguard = (KeyguardManager?)context.GetSystemService(Android.Content.Context.KeyguardService);
        return Task.FromResult(new BiometricAvailability(
            BiometricMapper.FromAndroidCanAuthenticate(code),
            GetSensors(context.PackageManager),
            keyguard?.IsDeviceSecure ?? false
        ));
    }


    public async Task<BiometricResult> Authenticate(BiometricRequest request, CancellationToken cancellationToken = default)
    {
        BiometricMapper.Validate(request);
        cancellationToken.ThrowIfCancellationRequested();

        var activity = await this.GetPromptActivity(cancellationToken).ConfigureAwait(false);
        var authenticators = BiometricMapper.ToAndroidAuthenticators(request.Strength, request.AllowDeviceCredential, (int)Build.VERSION.SdkInt);
        var builder = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle(request.Title ?? request.Reason)
            .SetAllowedAuthenticators(authenticators)
            .SetConfirmationRequired(request.ConfirmationRequired);

        if (request.Title != null)
            builder.SetDescription(request.Reason);

        if (request.Subtitle != null)
            builder.SetSubtitle(request.Subtitle);

        // a negative button is required without DEVICE_CREDENTIAL and rejected with it
        if (!request.AllowDeviceCredential)
            builder.SetNegativeButtonText(request.CancelText ?? activity.GetString(Android.Resource.String.Cancel));

        var info = builder.Build();
        var tcs = new TaskCompletionSource<BiometricResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        BiometricPrompt? prompt = null;

        platform.InvokeOnMainThread(() =>
        {
            try
            {
                prompt = new BiometricPrompt(activity, ContextCompat.GetMainExecutor(activity)!, new Callback(tcs));
                prompt.Authenticate(info);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        // cancelAuthentication reports ERROR_CANCELED through the callback, which completes the task
        await using var _ = cancellationToken
            .Register(() => platform.InvokeOnMainThread(() => prompt?.CancelAuthentication()))
            .ConfigureAwait(false);

        var result = await tcs.Task.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }


    static readonly TimeSpan ActivityTimeout = TimeSpan.FromSeconds(5);

    // Right after another activity returns (a file picker, the camera, a share sheet) its result arrives before the app's
    // own activity is back in front, so the last tracked activity can still be the one that just finished - often a plain
    // Activity, or the app's activity with its state still saved (where BiometricPrompt silently does nothing).
    // Wait a short moment for a FragmentActivity that can host the prompt instead of failing straight away.
    async Task<FragmentActivity> GetPromptActivity(CancellationToken cancellationToken)
    {
        var activity = AsPromptHost(platform.CurrentActivity);
        if (activity != null)
            return activity;

        var tcs = new TaskCompletionSource<FragmentActivity>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnActivityChanged(object? sender, ActivityChanged e)
        {
            if (e.State == ActivityState.Resumed && AsPromptHost(e.Activity) is { } host)
                tcs.TrySetResult(host);
        }

        platform.ActivityChanged += OnActivityChanged;
        try
        {
            // it may have resumed between the first check and the subscription
            activity = AsPromptHost(platform.CurrentActivity);
            if (activity != null)
                return activity;

            return await tcs.Task.WaitAsync(ActivityTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException("BiometricPrompt needs the current activity to be a FragmentActivity (AppCompatActivity / MauiAppCompatActivity) and none was resumed within " + ActivityTimeout.TotalSeconds + " seconds", ex);
        }
        finally
        {
            platform.ActivityChanged -= OnActivityChanged;
        }
    }


    static FragmentActivity? AsPromptHost(Activity? activity) =>
        activity is FragmentActivity host && !host.IsFinishing && !host.IsDestroyed && !host.SupportFragmentManager.IsStateSaved
            ? host
            : null;


    static BiometricType GetSensors(PackageManager? pm)
    {
        if (pm == null)
            return BiometricType.None;

        var types = BiometricType.None;
        if (pm.HasSystemFeature(PackageManager.FeatureFingerprint))
            types |= BiometricType.Fingerprint;

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            if (pm.HasSystemFeature(PackageManager.FeatureFace))
                types |= BiometricType.Face;

            if (pm.HasSystemFeature(PackageManager.FeatureIris))
                types |= BiometricType.Iris;
        }
        return types;
    }


    class Callback(TaskCompletionSource<BiometricResult> tcs) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
            => tcs.TrySetResult(BiometricResult.Succeeded);

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
            => tcs.TrySetResult(new BiometricResult(BiometricMapper.FromAndroidError(errorCode), errString?.ToString()));

        // a single non-matching attempt - the prompt stays up and the user can try again
        public override void OnAuthenticationFailed() { }
    }
}

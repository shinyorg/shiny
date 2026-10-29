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

        if (platform.CurrentActivity is not FragmentActivity activity)
            throw new InvalidOperationException("BiometricPrompt needs the current activity to be a FragmentActivity (AppCompatActivity / MauiAppCompatActivity)");

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

using Foundation;
using LocalAuthentication;
using Shiny.Biometrics.Infrastructure;

namespace Shiny.Biometrics;


/// <summary>
/// LocalAuthentication on iOS, Mac Catalyst and macOS. Biometrics-only requests evaluate
/// <c>deviceOwnerAuthenticationWithBiometrics</c>; <see cref="BiometricRequest.AllowDeviceCredential"/> switches to
/// <c>deviceOwnerAuthentication</c>, which also accepts the passcode (and a paired Apple Watch on macOS).
/// </summary>
public class AppleBiometrics : IBiometrics
{
    public Task<BiometricAvailability> GetAvailability(BiometricStrength strength = BiometricStrength.Strong, CancellationToken cancellationToken = default)
    {
        using var context = new LAContext();

        // biometryType is only filled in once canEvaluatePolicy has run
        var canBiometric = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var error);
        var types = BiometricMapper.FromAppleBiometryType((long)context.BiometryType);
        var passcode = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out _);

        var status = canBiometric
            ? BiometricStatus.Available
            : BiometricMapper.FromAppleAvailabilityError(error?.Code ?? BiometricMapper.AppleBiometryNotAvailable);

        return Task.FromResult(new BiometricAvailability(status, types, passcode));
    }


    public async Task<BiometricResult> Authenticate(BiometricRequest request, CancellationToken cancellationToken = default)
    {
        BiometricMapper.Validate(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var context = new LAContext();
        if (request.CancelText != null)
            context.LocalizedCancelTitle = request.CancelText;

        if (request.FallbackText != null)
            context.LocalizedFallbackTitle = request.FallbackText;

        var policy = request.AllowDeviceCredential
            ? LAPolicy.DeviceOwnerAuthentication
            : LAPolicy.DeviceOwnerAuthenticationWithBiometrics;

        if (context.CanEvaluatePolicy(policy, out _) && context.BiometryType == LABiometryType.FaceId)
            EnsureFaceIdUsageDescription();

        // invalidating the context dismisses the prompt and completes the evaluation with LAErrorAppCancel
        await using var registration = cancellationToken.Register(context.Invalidate).ConfigureAwait(false);
        var (success, error) = await context.EvaluatePolicyAsync(policy, request.Reason).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (success)
            return BiometricResult.Succeeded;

        return new BiometricResult(
            BiometricMapper.FromAppleError(error?.Code ?? BiometricMapper.AppleAuthenticationFailed),
            error?.LocalizedDescription
        );
    }


    static void EnsureFaceIdUsageDescription()
    {
        // without it, evaluating on a Face ID device terminates the app for a privacy violation
        if (NSBundle.MainBundle.ObjectForInfoDictionary("NSFaceIDUsageDescription") == null)
            throw new InvalidOperationException("Info.plist is missing NSFaceIDUsageDescription, which Face ID requires");
    }
}

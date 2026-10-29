using System.Diagnostics;
using Shiny.Biometrics.Infrastructure;
using Windows.Security.Credentials.UI;

namespace Shiny.Biometrics;


/// <summary>
/// Windows Hello through <see cref="UserConsentVerifier"/>. Hello accepts face, fingerprint or the Hello PIN and does
/// not say which was used, so <see cref="BiometricAvailability.Types"/> is <see cref="BiometricType.Unknown"/> and
/// <see cref="BiometricRequest.AllowDeviceCredential"/> cannot turn the PIN off.
/// </summary>
public class WindowsBiometrics : IBiometrics
{
    public async Task<BiometricAvailability> GetAvailability(BiometricStrength strength = BiometricStrength.Strong, CancellationToken cancellationToken = default)
    {
        var availability = await UserConsentVerifier.CheckAvailabilityAsync().AsTask(cancellationToken).ConfigureAwait(false);
        var status = BiometricMapper.FromWindowsAvailability((int)availability);

        // a Hello device always has its PIN behind it
        return status == BiometricStatus.Available
            ? new BiometricAvailability(status, BiometricType.Unknown, true)
            : new BiometricAvailability(status);
    }


    public async Task<BiometricResult> Authenticate(BiometricRequest request, CancellationToken cancellationToken = default)
    {
        BiometricMapper.Validate(request);
        cancellationToken.ThrowIfCancellationRequested();

        // a desktop (WinUI / unpackaged) app has no CoreWindow, so the plain call can open the prompt behind the app -
        // parent it to the main window when there is one
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        var operation = hwnd != IntPtr.Zero
            ? UserConsentVerifierInterop.RequestVerificationForWindowAsync(hwnd, request.Reason)
            : UserConsentVerifier.RequestVerificationAsync(request.Reason);

        var result = await operation.AsTask(cancellationToken).ConfigureAwait(false);
        return new BiometricResult(BiometricMapper.FromWindowsResult((int)result));
    }
}

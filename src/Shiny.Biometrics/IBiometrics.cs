namespace Shiny.Biometrics;


/// <summary>
/// Verifies the device owner with the platform's own prompt - LocalAuthentication on iOS, Mac Catalyst and macOS (Touch
/// ID, Face ID, Optic ID, and a paired Apple Watch on macOS), AndroidX BiometricPrompt (fingerprint, face, iris) and
/// Windows Hello. The result is a local yes/no: use it to gate UI, not as a credential your server trusts.
/// </summary>
public interface IBiometrics
{
    /// <summary>
    /// What the device can do right now - hardware, enrollment, lockout and whether a passcode / PIN is set - with no UI.
    /// </summary>
    /// <param name="strength">Android only: the class of biometric the app will ask for. Apple biometrics are all strong,
    /// and Windows Hello does not grade them.</param>
    Task<BiometricAvailability> GetAvailability(BiometricStrength strength = BiometricStrength.Strong, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows the platform prompt. Failures (cancelled, locked out, not enrolled...) are returned as a
    /// <see cref="BiometricResult"/> rather than thrown.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="BiometricRequest.Reason"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">There is nothing to show the prompt from (no current Android
    /// FragmentActivity), or the iOS app is missing <c>NSFaceIDUsageDescription</c> on a Face ID device.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled - the prompt is
    /// dismissed.</exception>
    Task<BiometricResult> Authenticate(BiometricRequest request, CancellationToken cancellationToken = default);
}


public enum BiometricStrength
{
    /// <summary>Android Class 3 (<c>BIOMETRIC_STRONG</c>) - the only class Android lets unlock Keystore keys.</summary>
    Strong,

    /// <summary>Android Class 2 (<c>BIOMETRIC_WEAK</c>) - includes most camera-based face unlock.</summary>
    Weak
}


/// <summary>The biometric hardware the device has. Apple reports exactly one; Android reports every sensor it declares.</summary>
[Flags]
public enum BiometricType
{
    None = 0,

    /// <summary>Touch ID, or an Android fingerprint sensor.</summary>
    Fingerprint = 1,

    /// <summary>Face ID, or Android face unlock.</summary>
    Face = 2,

    /// <summary>Optic ID, or an Android iris scanner.</summary>
    Iris = 4,

    /// <summary>Biometrics are present but the platform does not say which - Windows Hello, or an Apple biometry type
    /// newer than this library.</summary>
    Unknown = 8
}


public enum BiometricStatus
{
    /// <summary>Biometrics can be used now.</summary>
    Available,

    /// <summary>No biometric hardware, or the platform has no biometric API (Linux, Blazor, plain .NET).</summary>
    NotSupported,

    /// <summary>The hardware exists but no fingerprint / face is enrolled (Windows: Hello is not set up).</summary>
    NotEnrolled,

    /// <summary>Too many failed attempts - the user must unlock with their passcode first.</summary>
    LockedOut,

    /// <summary>Apple requires a device passcode before biometrics can be used.</summary>
    PasscodeNotSet,

    /// <summary>The hardware is busy, disconnected or disabled by policy - try again later.</summary>
    Unavailable,

    /// <summary>Android: the sensor has a known vulnerability and needs a security update.</summary>
    SecurityUpdateRequired
}


/// <param name="Status">Whether biometrics can be used right now.</param>
/// <param name="Types">The hardware present - reported even when not enrolled or locked out, so the UI can say
/// "Set up Face ID".</param>
/// <param name="IsDeviceCredentialSet">A passcode / PIN / pattern is set, so
/// <see cref="BiometricRequest.AllowDeviceCredential"/> can fall back to it.</param>
public record BiometricAvailability(
    BiometricStatus Status,
    BiometricType Types = BiometricType.None,
    bool IsDeviceCredentialSet = false
)
{
    public static BiometricAvailability NotSupported { get; } = new(BiometricStatus.NotSupported);

    public bool IsAvailable => this.Status == BiometricStatus.Available;
}


public class BiometricRequest
{
    /// <summary>Why the app is asking. Apple shows it under the system title, Android uses it as the description (or
    /// the title when <see cref="Title"/> is null), Windows Hello shows it as the message.</summary>
    public required string Reason { get; init; }

    /// <summary>Android: the prompt title. Apple and Windows use their own.</summary>
    public string? Title { get; init; }

    /// <summary>Android: a line under the title.</summary>
    public string? Subtitle { get; init; }

    /// <summary>The cancel button. Android requires one when <see cref="AllowDeviceCredential"/> is off and defaults to
    /// the system "Cancel"; Windows ignores it.</summary>
    public string? CancelText { get; init; }

    /// <summary>
    /// Apple: the button shown after a failed match. Null keeps the system "Enter Password"; an empty string hides it.
    /// When <see cref="AllowDeviceCredential"/> is off, tapping it returns <see cref="BiometricResultStatus.Fallback"/>
    /// so the app can show its own login. Android and Windows ignore it.
    /// </summary>
    public string? FallbackText { get; init; }

    /// <summary>
    /// Let the user use their passcode / PIN / pattern instead of a biometric. Windows Hello always offers its PIN, so
    /// this cannot be turned off there.
    /// </summary>
    public bool AllowDeviceCredential { get; init; }

    /// <summary>Android: the class of biometric accepted.</summary>
    public BiometricStrength Strength { get; init; } = BiometricStrength.Strong;

    /// <summary>Android: for passive biometrics (face), wait for the user to confirm after a match.</summary>
    public bool ConfirmationRequired { get; init; } = true;
}


public enum BiometricResultStatus
{
    /// <summary>The device owner was verified.</summary>
    Success,

    /// <summary>The user or the system dismissed the prompt (including the Android cancel / negative button).</summary>
    Cancelled,

    /// <summary>Apple: the user tapped <see cref="BiometricRequest.FallbackText"/> - show your own login.</summary>
    Fallback,

    /// <summary>Verification failed - Apple after its attempts ran out, Android when the sensor could not process,
    /// Windows when its retries were exhausted.</summary>
    Failed,

    /// <summary>Too many failed attempts - biometrics stay locked until the user enters their passcode.</summary>
    LockedOut,

    /// <summary>Nothing enrolled.</summary>
    NotEnrolled,

    /// <summary>A device credential was required (or allowed as the only option) but none is set.</summary>
    PasscodeNotSet,

    /// <summary>No hardware, the hardware is busy or disabled, or the platform has no biometric API.</summary>
    NotAvailable,

    /// <summary>Android: the prompt timed out.</summary>
    Timeout
}


/// <param name="Status">The outcome.</param>
/// <param name="ErrorMessage">The platform's own (localized) message for a failure, when it gives one.</param>
public record BiometricResult(BiometricResultStatus Status, string? ErrorMessage = null)
{
    public static BiometricResult Succeeded { get; } = new(BiometricResultStatus.Success);
    public static BiometricResult NotAvailable { get; } = new(BiometricResultStatus.NotAvailable);

    public bool IsSuccess => this.Status == BiometricResultStatus.Success;
}

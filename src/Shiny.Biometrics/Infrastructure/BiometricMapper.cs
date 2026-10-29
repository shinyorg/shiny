namespace Shiny.Biometrics.Infrastructure;


/// <summary>
/// Translates the platforms' raw status / error codes into the library's enums, and picks Android's authenticator
/// flags. Compiled on every target framework so it is unit tested on net10.0; the platform classes only call the native
/// APIs and pass the codes through.
/// </summary>
static class BiometricMapper
{
    public static void Validate(BiometricRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (String.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A reason is required - every platform shows it in the prompt", nameof(request));
    }


    #region Apple (LAError codes)

    public const long AppleAuthenticationFailed = -1;
    public const long AppleUserCancel = -2;
    public const long AppleUserFallback = -3;
    public const long AppleSystemCancel = -4;
    public const long ApplePasscodeNotSet = -5;
    public const long AppleBiometryNotAvailable = -6;
    public const long AppleBiometryNotEnrolled = -7;
    public const long AppleBiometryLockout = -8;
    public const long AppleAppCancel = -9;
    public const long AppleInvalidContext = -10;
    public const long AppleCompanionNotAvailable = -11;
    public const long AppleBiometryNotPaired = -12;
    public const long AppleBiometryDisconnected = -13;
    public const long AppleNotInteractive = -1004;

    // LABiometryType
    public const long AppleBiometryNone = 0;
    public const long AppleBiometryTouchId = 1;
    public const long AppleBiometryFaceId = 2;
    public const long AppleBiometryOpticId = 4;


    public static BiometricType FromAppleBiometryType(long type) => type switch
    {
        AppleBiometryNone => BiometricType.None,
        AppleBiometryTouchId => BiometricType.Fingerprint,
        AppleBiometryFaceId => BiometricType.Face,
        AppleBiometryOpticId => BiometricType.Iris,
        _ => BiometricType.Unknown
    };


    // the error canEvaluatePolicy gives for the biometrics-only policy
    public static BiometricStatus FromAppleAvailabilityError(long code) => code switch
    {
        AppleBiometryNotEnrolled => BiometricStatus.NotEnrolled,
        AppleBiometryLockout => BiometricStatus.LockedOut,
        ApplePasscodeNotSet => BiometricStatus.PasscodeNotSet,
        AppleBiometryNotPaired or AppleBiometryDisconnected or AppleCompanionNotAvailable => BiometricStatus.Unavailable,
        _ => BiometricStatus.NotSupported
    };


    public static BiometricResultStatus FromAppleError(long code) => code switch
    {
        AppleUserCancel or AppleSystemCancel or AppleAppCancel => BiometricResultStatus.Cancelled,
        AppleUserFallback => BiometricResultStatus.Fallback,
        AppleAuthenticationFailed => BiometricResultStatus.Failed,
        AppleBiometryLockout => BiometricResultStatus.LockedOut,
        AppleBiometryNotEnrolled => BiometricResultStatus.NotEnrolled,
        ApplePasscodeNotSet => BiometricResultStatus.PasscodeNotSet,
        _ => BiometricResultStatus.NotAvailable
    };

    #endregion


    #region Android (BiometricManager / BiometricPrompt)

    public const int AndroidBiometricStrong = 0x000F;
    public const int AndroidBiometricWeak = 0x00FF;
    public const int AndroidDeviceCredential = 0x8000;

    // BiometricManager.canAuthenticate
    public const int AndroidSuccess = 0;
    public const int AndroidStatusUnknown = -1;
    public const int AndroidErrorUnsupported = -2;
    public const int AndroidErrorHwUnavailable = 1;
    public const int AndroidErrorNoneEnrolled = 11;
    public const int AndroidErrorNoHardware = 12;
    public const int AndroidErrorSecurityUpdateRequired = 15;

    // BiometricPrompt.ERROR_*
    public const int AndroidPromptHwUnavailable = 1;
    public const int AndroidPromptUnableToProcess = 2;
    public const int AndroidPromptTimeout = 3;
    public const int AndroidPromptNoSpace = 4;
    public const int AndroidPromptCanceled = 5;
    public const int AndroidPromptLockout = 7;
    public const int AndroidPromptVendor = 8;
    public const int AndroidPromptLockoutPermanent = 9;
    public const int AndroidPromptUserCanceled = 10;
    public const int AndroidPromptNoBiometrics = 11;
    public const int AndroidPromptHwNotPresent = 12;
    public const int AndroidPromptNegativeButton = 13;
    public const int AndroidPromptNoDeviceCredential = 14;
    public const int AndroidPromptSecurityUpdateRequired = 15;


    /// <summary>
    /// The <c>setAllowedAuthenticators</c> flags. Before API 30, <c>BIOMETRIC_STRONG | DEVICE_CREDENTIAL</c> is rejected
    /// on API 28-29, so a strong request that allows the device credential is relaxed to weak there - the credential
    /// fallback is the weaker link either way.
    /// </summary>
    public static int ToAndroidAuthenticators(BiometricStrength strength, bool allowDeviceCredential, int sdkInt)
    {
        var biometric = strength == BiometricStrength.Weak ? AndroidBiometricWeak : AndroidBiometricStrong;
        if (!allowDeviceCredential)
            return biometric;

        if (biometric == AndroidBiometricStrong && sdkInt is 28 or 29)
            biometric = AndroidBiometricWeak;

        return biometric | AndroidDeviceCredential;
    }


    public static BiometricStatus FromAndroidCanAuthenticate(int code) => code switch
    {
        AndroidSuccess => BiometricStatus.Available,
        AndroidErrorNoneEnrolled => BiometricStatus.NotEnrolled,
        AndroidErrorHwUnavailable => BiometricStatus.Unavailable,
        AndroidErrorSecurityUpdateRequired => BiometricStatus.SecurityUpdateRequired,
        _ => BiometricStatus.NotSupported // NO_HARDWARE, UNSUPPORTED, STATUS_UNKNOWN
    };


    public static BiometricResultStatus FromAndroidError(int code) => code switch
    {
        AndroidPromptCanceled or AndroidPromptUserCanceled or AndroidPromptNegativeButton => BiometricResultStatus.Cancelled,
        AndroidPromptTimeout => BiometricResultStatus.Timeout,
        AndroidPromptLockout or AndroidPromptLockoutPermanent => BiometricResultStatus.LockedOut,
        AndroidPromptNoBiometrics => BiometricResultStatus.NotEnrolled,
        AndroidPromptNoDeviceCredential => BiometricResultStatus.PasscodeNotSet,
        AndroidPromptUnableToProcess or AndroidPromptNoSpace or AndroidPromptVendor => BiometricResultStatus.Failed,
        _ => BiometricResultStatus.NotAvailable // HW_UNAVAILABLE, HW_NOT_PRESENT, SECURITY_UPDATE_REQUIRED
    };

    #endregion


    #region Windows (UserConsentVerifierAvailability / UserConsentVerificationResult)

    public const int WindowsAvailable = 0;
    public const int WindowsDeviceNotPresent = 1;
    public const int WindowsNotConfiguredForUser = 2;
    public const int WindowsDisabledByPolicy = 3;
    public const int WindowsDeviceBusy = 4;
    public const int WindowsRetriesExhausted = 5;
    public const int WindowsCanceled = 6;


    public static BiometricStatus FromWindowsAvailability(int availability) => availability switch
    {
        WindowsAvailable => BiometricStatus.Available,
        WindowsNotConfiguredForUser => BiometricStatus.NotEnrolled,
        WindowsDisabledByPolicy or WindowsDeviceBusy => BiometricStatus.Unavailable,
        _ => BiometricStatus.NotSupported
    };


    public static BiometricResultStatus FromWindowsResult(int result) => result switch
    {
        WindowsAvailable => BiometricResultStatus.Success, // Verified
        WindowsCanceled => BiometricResultStatus.Cancelled,
        WindowsRetriesExhausted => BiometricResultStatus.Failed,
        WindowsNotConfiguredForUser => BiometricResultStatus.NotEnrolled,
        _ => BiometricResultStatus.NotAvailable
    };

    #endregion
}

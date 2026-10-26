using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.AgeSignals.Infrastructure;


/// <summary>
/// Translates what the stores return into <see cref="AgeSignal"/>. Compiled on every target framework so the mapping is
/// unit tested on net10.0; the platform classes only call the native APIs.
/// </summary>
static class AgeSignalMapper
{
    public static void ValidateGates(int gate1, int? gate2, int? gate3)
    {
        if (gate1 < 1)
            throw new ArgumentOutOfRangeException(nameof(gate1), gate1, "Age gates must be positive");

        if (gate2 != null && gate2 <= gate1)
            throw new ArgumentOutOfRangeException(nameof(gate2), gate2, "Age gates must be ascending");

        if (gate3 != null && (gate2 == null || gate3 <= gate2))
            throw new ArgumentOutOfRangeException(nameof(gate3), gate3, "Age gates must be ascending, with gate2 set before gate3");
    }


    #region Apple

    public static AgeSignalsEligibility FromAppleEligibility(string json)
    {
        var native = JsonSerializer.Deserialize(json, AgeSignalsJsonContext.Default.AppleEligibility)
            ?? new AppleEligibility();

        var features = native.RegulatoryFeatures;
        return new AgeSignalsEligibility(
            true,
            native.IsEligible,
            features?.Contains("declaredAgeRangeRequired"),
            features?.Contains("significantAppChangeRequiresParentalConsent"),
            features?.Contains("significantAppChangeRequiresAdultNotification")
        );
    }


    public static AgeSignal FromAppleRange(string json)
    {
        var native = JsonSerializer.Deserialize(json, AgeSignalsJsonContext.Default.AppleAgeRange)
            ?? new AppleAgeRange();

        if (native.Status != "shared")
            return new AgeSignal(AgeSignalStatus.Declined);

        return new AgeSignal(
            AgeSignalStatus.Shared,
            native.LowerBound,
            native.UpperBound,
            FromAppleDeclaration(native.Declaration),
            native.CommunicationLimits
        );
    }


    // the Swift case name - every 26.2 "*Checked*" case is deprecated, renamed to "confirmed" in 26.5
    public static AgeRangeSource FromAppleDeclaration(string? declaration) => declaration switch
    {
        null => AgeRangeSource.Unknown,
        "selfDeclared" => AgeRangeSource.SelfDeclared,
        "guardianDeclared" => AgeRangeSource.GuardianDeclared,
        _ when declaration.Contains("checked", StringComparison.OrdinalIgnoreCase) || declaration == "confirmed" => AgeRangeSource.Confirmed,
        _ => AgeRangeSource.Unknown
    };


    /// <summary>
    /// A native error: a status when it isn't a failure (the user declined onboarding, the OS is too old), otherwise thrown.
    /// </summary>
    public static AgeSignal FromAppleError(string json)
    {
        AppleError? native = null;
        try
        {
            native = JsonSerializer.Deserialize(json, AgeSignalsJsonContext.Default.AppleError);
        }
        catch (JsonException)
        {
        }
        native ??= new AppleError { Message = json };
        var message = native.Message ?? native.Code ?? "Unknown Declared Age Range error";

        return native.Code switch
        {
            "declinedOnboarding" => new AgeSignal(AgeSignalStatus.Declined),
            "unsupported" => AgeSignal.NotSupported,
            "invalidRequest" => throw new ArgumentException(message),
            _ => throw ToAppleException(native.Code, message)
        };
    }


    public static AgeSignalsException ToAppleException(string? code, string message) => new(code switch
    {
        "notAvailable" => AgeSignalsErrorReason.NotAvailable,
        "invalidAccount" => AgeSignalsErrorReason.InvalidAccount,
        "network" => AgeSignalsErrorReason.Network,
        "noWindow" => AgeSignalsErrorReason.NoUserInterface,
        _ => AgeSignalsErrorReason.Unknown
    }, message);

    #endregion

    #region Google Play

    // com.google.android.play.agesignals.model.* (age-signals 0.0.4)
    public const int PlayStatusShared = 1;
    public const int PlayStatusNotShared = 2;
    public const int PlayStatusVerificationRequired = 3;


    public static AgeSignal FromPlayAccessStatus(int? ageSignalsStatus) => ageSignalsStatus switch
    {
        PlayStatusVerificationRequired => new AgeSignal(AgeSignalStatus.VerificationRequired),
        _ => new AgeSignal(AgeSignalStatus.Declined)
    };


    public static AgeSignal FromPlayResult(
        int? ageLower,
        int? ageUpper,
        int? ageRangeSource,
        int? significantChangeStatus,
        DateTimeOffset? significantChangeApprovalDate,
        string? installId
    )
    {
        // Play's own docs: both bounds null means the user is not sharing
        if (ageLower == null && ageUpper == null)
            return new AgeSignal(AgeSignalStatus.Declined);

        return new AgeSignal(
            AgeSignalStatus.Shared,
            ageLower,
            ageUpper,
            ageRangeSource switch
            {
                1 => AgeRangeSource.SelfDeclared,
                2 => AgeRangeSource.GuardianDeclared,
                3 => AgeRangeSource.Confirmed,
                4 => AgeRangeSource.IdVerified,
                _ => AgeRangeSource.Unknown
            },
            false,
            significantChangeStatus switch
            {
                1 => SignificantChangeStatus.Approved,
                2 => SignificantChangeStatus.Pending,
                3 => SignificantChangeStatus.Declined,
                _ => null
            },
            significantChangeApprovalDate,
            installId
        );
    }


    public static AgeSignalsException FromPlayError(int errorCode, string? message, Exception? inner = null) => new(
        errorCode switch
        {
            -1 => AgeSignalsErrorReason.NotAvailable,                // API_NOT_AVAILABLE
            -3 => AgeSignalsErrorReason.Network,                     // NETWORK_ERROR
            -5 or -8 => AgeSignalsErrorReason.Transient,             // CANNOT_BIND_TO_SERVICE, CLIENT_TRANSIENT_ERROR
            -2 or -4 or -6 or -7 or -10 => AgeSignalsErrorReason.StoreUnavailable, // store/services missing or outdated, SDK outdated
            -9 => AgeSignalsErrorReason.AppNotOwned,                 // APP_NOT_OWNED
            _ => AgeSignalsErrorReason.Unknown                       // INTERNAL_ERROR (-100)
        },
        message ?? $"Play Age Signals error {errorCode}",
        errorCode,
        inner
    );

    #endregion
}


sealed class AppleEligibility
{
    public bool? IsEligible { get; set; }
    public List<string>? RegulatoryFeatures { get; set; }
}


sealed class AppleAgeRange
{
    public string? Status { get; set; }
    public int? LowerBound { get; set; }
    public int? UpperBound { get; set; }
    public string? Declaration { get; set; }
    public bool CommunicationLimits { get; set; }
}


sealed class AppleError
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppleEligibility))]
[JsonSerializable(typeof(AppleAgeRange))]
[JsonSerializable(typeof(AppleError))]
partial class AgeSignalsJsonContext : JsonSerializerContext;

namespace Shiny.AgeSignals;


/// <summary>
/// The user's age range as the app store shares it - Apple's Declared Age Range (iOS / macOS 26+) and Google Play Age
/// Signals - for app store accountability laws (Texas SB 2420, Utah, Louisiana, California AB 1043) and any app that
/// gates features by age. Nothing is cached: consent can be revoked and a birthday can cross a band, so ask at the
/// moments the law cares about (first launch, before a gated feature, after an update).
/// </summary>
public interface IAgeSignals
{
    /// <summary>
    /// Whether the platform has a store age API, and what the store says applies to this user - with no UI. Values the
    /// platform cannot answer are null: Android has no eligibility API, and Apple added
    /// <see cref="AgeSignalsEligibility.IsEligible"/> in 26.2 and the regulatory flags in 26.4.
    /// </summary>
    Task<AgeSignalsEligibility> GetEligibility(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the store for the user's age range. Apple may present a sheet. Google Play shows its sharing prompt only
    /// outside the US law states (there, users verify in the Play Store itself).
    /// </summary>
    /// <param name="gate1">Apple age gate - the ages the returned range is bounded by (e.g. 13, 16, 18), ascending.
    /// Google Play ignores these: its bands are 0-12, 13-15, 16-17 and 18+ unless you set custom minimum ages in Play
    /// Console - configure the same ages there.</param>
    /// <param name="gate2">Optional second Apple age gate, above <paramref name="gate1"/>.</param>
    /// <param name="gate3">Optional third Apple age gate, above <paramref name="gate2"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The gates are not positive and ascending.</exception>
    /// <exception cref="AgeSignalsException">The store could not answer - see <see cref="AgeSignalsException.Reason"/>.</exception>
    Task<AgeSignal> RequestAgeRange(int gate1, int? gate2 = null, int? gate3 = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows Apple's significant-update acknowledgment (iOS 26.4+), which tells a supervised child's guardian about a
    /// significant change to the app. False where the platform has no such sheet: on Google Play, declare significant
    /// changes in Play Console and read <see cref="AgeSignal.SignificantChange"/> instead.
    /// </summary>
    Task<bool> ShowSignificantUpdate(string updateDescription, CancellationToken cancellationToken = default);
}


/// <param name="IsSupported">The platform has a store age API - iOS / macOS 26+, or Android.</param>
/// <param name="IsEligible">Apple <c>isEligibleForAgeFeatures</c> (26.2+): the user is in a region or account where age
/// features apply. Null on Android and earlier OS versions.</param>
/// <param name="AgeRangeRequired">Apple <c>declaredAgeRangeRequired</c> (26.4+): the app must obtain the age range.</param>
/// <param name="SignificantChangeRequiresParentalConsent">Apple (26.4+): significant changes need a guardian's consent.</param>
/// <param name="SignificantChangeRequiresAdultNotification">Apple (26.4+): significant changes must be acknowledged
/// by adults - see <see cref="IAgeSignals.ShowSignificantUpdate"/>.</param>
public record AgeSignalsEligibility(
    bool IsSupported,
    bool? IsEligible = null,
    bool? AgeRangeRequired = null,
    bool? SignificantChangeRequiresParentalConsent = null,
    bool? SignificantChangeRequiresAdultNotification = null
)
{
    public static AgeSignalsEligibility NotSupported { get; } = new(false);
}


public enum AgeSignalStatus
{
    /// <summary>No store age API on this platform or OS version.</summary>
    NotSupported,

    /// <summary>The user (or their guardian) declined to share an age range.</summary>
    Declined,

    /// <summary>Google Play: the user is in a jurisdiction where verification is mandatory and their age is unknown -
    /// ask them to open the Play Store to verify or set up supervision.</summary>
    VerificationRequired,

    /// <summary>An age range was shared.</summary>
    Shared
}


/// <summary>
/// How the shared age range was established, weakest first, so an app can require a minimum
/// (<c>signal.Source &gt;= AgeRangeSource.Confirmed</c>).
/// </summary>
public enum AgeRangeSource
{
    /// <summary>Not shared, or the store did not say.</summary>
    Unknown,

    /// <summary>The user declared it themselves (Apple <c>selfDeclared</c>, Play <c>TIER_A</c>).</summary>
    SelfDeclared,

    /// <summary>A parent or guardian manages it - the account is supervised (Apple <c>guardianDeclared</c>, Play <c>TIER_B</c>).</summary>
    GuardianDeclared,

    /// <summary>Checked by another method - payment card, ID, age estimation (Apple <c>confirmed</c> and the 26.2
    /// <c>*Checked</c> cases it replaced, Play <c>TIER_C</c>).</summary>
    Confirmed,

    /// <summary>Government ID plus a selfie, or a digital ID (Play <c>TIER_D</c>; Apple reports these as <see cref="Confirmed"/>).</summary>
    IdVerified
}


/// <summary>Google Play: a guardian's approval of the app's significant changes.</summary>
public enum SignificantChangeStatus
{
    /// <summary>The most recent significant change, and every one before it, is approved.</summary>
    Approved,

    /// <summary>The guardian has not yet approved one or more significant changes.</summary>
    Pending,

    /// <summary>The guardian declined one or more significant changes.</summary>
    Declined
}


/// <param name="Status">Whether a range was shared.</param>
/// <param name="LowerBound">Inclusive lower bound; null when not shared (Apple also leaves it null for the lowest band).</param>
/// <param name="UpperBound">Inclusive upper bound; null when not shared or for the highest band (e.g. 18+).</param>
/// <param name="Source">How the range was established.</param>
/// <param name="CommunicationLimits">Apple: a guardian has communication limits turned on for this child.</param>
/// <param name="SignificantChange">Google Play: guardian approval of significant changes; null for unsupervised
/// accounts, supervised ones with no changes recorded, and on Apple.</param>
/// <param name="SignificantChangeApprovedThrough">Google Play: the effective date of the latest approved significant
/// change - everything effective before it is approved too.</param>
/// <param name="InstallId">Google Play: the id Play assigns to a supervised install, used to match revoked-approval
/// notifications on your server.</param>
public record AgeSignal(
    AgeSignalStatus Status,
    int? LowerBound = null,
    int? UpperBound = null,
    AgeRangeSource Source = AgeRangeSource.Unknown,
    bool CommunicationLimits = false,
    SignificantChangeStatus? SignificantChange = null,
    DateTimeOffset? SignificantChangeApprovedThrough = null,
    string? InstallId = null
)
{
    public static AgeSignal NotSupported { get; } = new(AgeSignalStatus.NotSupported);

    /// <summary>
    /// True when the store positively says the user is at least <paramref name="age"/>, false when it positively says
    /// they are younger, and null when it can't tell (nothing shared, or the range straddles the age).
    /// </summary>
    public bool? IsAtLeast(int age)
    {
        if (this.Status != AgeSignalStatus.Shared || (this.LowerBound == null && this.UpperBound == null))
            return null;

        if (this.LowerBound >= age)
            return true;

        if (this.UpperBound < age)
            return false;

        return null;
    }

    /// <summary><see cref="IsAtLeast"/> 18.</summary>
    public bool? IsAdult => this.IsAtLeast(18);
}

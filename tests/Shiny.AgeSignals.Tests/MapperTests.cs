using Shiny.AgeSignals.Infrastructure;
using Xunit;

namespace Shiny.AgeSignals.Tests;


public class MapperTests
{
    [Theory]
    [InlineData(13, null, null)]
    [InlineData(13, 16, null)]
    [InlineData(13, 16, 18)]
    public void ValidateGates_Accepts(int gate1, int? gate2, int? gate3)
        => AgeSignalMapper.ValidateGates(gate1, gate2, gate3);


    [Theory]
    [InlineData(0, null, null)]
    [InlineData(16, 13, null)]
    [InlineData(13, 13, null)]
    [InlineData(13, 16, 16)]
    [InlineData(13, null, 18)]
    public void ValidateGates_Rejects(int gate1, int? gate2, int? gate3)
        => Assert.Throws<ArgumentOutOfRangeException>(() => AgeSignalMapper.ValidateGates(gate1, gate2, gate3));


    [Fact]
    public void Apple_SharedRange()
    {
        var signal = AgeSignalMapper.FromAppleRange(
            """{"status":"shared","lowerBound":13,"upperBound":15,"declaration":"guardianDeclared","communicationLimits":true}"""
        );
        Assert.Equal(AgeSignalStatus.Shared, signal.Status);
        Assert.Equal(13, signal.LowerBound);
        Assert.Equal(15, signal.UpperBound);
        Assert.Equal(AgeRangeSource.GuardianDeclared, signal.Source);
        Assert.True(signal.CommunicationLimits);
        Assert.Null(signal.SignificantChange);
    }


    [Fact]
    public void Apple_Declined()
        => Assert.Equal(AgeSignalStatus.Declined, AgeSignalMapper.FromAppleRange("""{"status":"declined"}""").Status);


    [Theory]
    [InlineData(null, AgeRangeSource.Unknown)]
    [InlineData("selfDeclared", AgeRangeSource.SelfDeclared)]
    [InlineData("guardianDeclared", AgeRangeSource.GuardianDeclared)]
    [InlineData("confirmed", AgeRangeSource.Confirmed)]
    [InlineData("governmentIDChecked", AgeRangeSource.Confirmed)]
    [InlineData("guardianPaymentChecked", AgeRangeSource.Confirmed)]
    [InlineData("checkedByOtherMethod", AgeRangeSource.Confirmed)]
    [InlineData("somethingFromIos28", AgeRangeSource.Unknown)]
    public void Apple_Declaration(string? declaration, AgeRangeSource expected)
        => Assert.Equal(expected, AgeSignalMapper.FromAppleDeclaration(declaration));


    [Fact]
    public void Apple_Eligibility_Full()
    {
        var result = AgeSignalMapper.FromAppleEligibility(
            """{"isEligible":true,"regulatoryFeatures":["declaredAgeRangeRequired","significantAppChangeRequiresParentalConsent"]}"""
        );
        Assert.True(result.IsSupported);
        Assert.True(result.IsEligible);
        Assert.True(result.AgeRangeRequired);
        Assert.True(result.SignificantChangeRequiresParentalConsent);
        Assert.False(result.SignificantChangeRequiresAdultNotification);
    }


    [Fact]
    public void Apple_Eligibility_OlderOs_LeavesUnknownsNull()
    {
        // iOS 26.0 / 26.1: neither property exists
        var result = AgeSignalMapper.FromAppleEligibility("{}");
        Assert.True(result.IsSupported);
        Assert.Null(result.IsEligible);
        Assert.Null(result.AgeRangeRequired);
        Assert.Null(result.SignificantChangeRequiresParentalConsent);
        Assert.Null(result.SignificantChangeRequiresAdultNotification);
    }


    [Fact]
    public void Apple_Error_DeclinedOnboarding_IsStatus()
        => Assert.Equal(AgeSignalStatus.Declined, AgeSignalMapper.FromAppleError("""{"code":"declinedOnboarding"}""").Status);


    [Fact]
    public void Apple_Error_Unsupported_IsStatus()
        => Assert.Equal(AgeSignalStatus.NotSupported, AgeSignalMapper.FromAppleError("""{"code":"unsupported"}""").Status);


    [Fact]
    public void Apple_Error_InvalidRequest_IsArgumentException()
        => Assert.Throws<ArgumentException>(() => AgeSignalMapper.FromAppleError("""{"code":"invalidRequest","message":"bad gates"}"""));


    [Theory]
    [InlineData("notAvailable", AgeSignalsErrorReason.NotAvailable)]
    [InlineData("invalidAccount", AgeSignalsErrorReason.InvalidAccount)]
    [InlineData("network", AgeSignalsErrorReason.Network)]
    [InlineData("noWindow", AgeSignalsErrorReason.NoUserInterface)]
    [InlineData("unknown", AgeSignalsErrorReason.Unknown)]
    public void Apple_Error_Throws(string code, AgeSignalsErrorReason reason)
    {
        var ex = Assert.Throws<AgeSignalsException>(() => AgeSignalMapper.FromAppleError($$"""{"code":"{{code}}","message":"m"}"""));
        Assert.Equal(reason, ex.Reason);
        Assert.Equal("m", ex.Message);
    }


    [Fact]
    public void Apple_Error_NotJson_Throws()
    {
        var ex = Assert.Throws<AgeSignalsException>(() => AgeSignalMapper.FromAppleError("garbage"));
        Assert.Equal(AgeSignalsErrorReason.Unknown, ex.Reason);
    }


    [Theory]
    [InlineData(AgeSignalMapper.PlayStatusNotShared, AgeSignalStatus.Declined)]
    [InlineData(AgeSignalMapper.PlayStatusVerificationRequired, AgeSignalStatus.VerificationRequired)]
    [InlineData(0, AgeSignalStatus.Declined)]
    [InlineData(null, AgeSignalStatus.Declined)]
    public void Play_AccessStatus(int? status, AgeSignalStatus expected)
        => Assert.Equal(expected, AgeSignalMapper.FromPlayAccessStatus(status).Status);


    [Fact]
    public void Play_SupervisedMinor()
    {
        var approved = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var signal = AgeSignalMapper.FromPlayResult(13, 15, 2, 1, approved, "abc123xyz789");

        Assert.Equal(AgeSignalStatus.Shared, signal.Status);
        Assert.Equal(13, signal.LowerBound);
        Assert.Equal(15, signal.UpperBound);
        Assert.Equal(AgeRangeSource.GuardianDeclared, signal.Source);
        Assert.Equal(SignificantChangeStatus.Approved, signal.SignificantChange);
        Assert.Equal(approved, signal.SignificantChangeApprovedThrough);
        Assert.Equal("abc123xyz789", signal.InstallId);
        Assert.False(signal.IsAdult);
    }


    [Theory]
    [InlineData(1, AgeRangeSource.SelfDeclared)]
    [InlineData(2, AgeRangeSource.GuardianDeclared)]
    [InlineData(3, AgeRangeSource.Confirmed)]
    [InlineData(4, AgeRangeSource.IdVerified)]
    [InlineData(0, AgeRangeSource.Unknown)]
    [InlineData(null, AgeRangeSource.Unknown)]
    public void Play_Tiers(int? tier, AgeRangeSource expected)
        => Assert.Equal(expected, AgeSignalMapper.FromPlayResult(18, null, tier, null, null, null).Source);


    [Theory]
    [InlineData(2, SignificantChangeStatus.Pending)]
    [InlineData(3, SignificantChangeStatus.Declined)]
    [InlineData(0, null)]
    [InlineData(null, null)]
    public void Play_SignificantChange(int? status, SignificantChangeStatus? expected)
        => Assert.Equal(expected, AgeSignalMapper.FromPlayResult(13, 15, 2, status, null, null).SignificantChange);


    [Fact]
    public void Play_NoBounds_IsDeclined()
        => Assert.Equal(AgeSignalStatus.Declined, AgeSignalMapper.FromPlayResult(null, null, null, null, null, null).Status);


    [Theory]
    [InlineData(-1, AgeSignalsErrorReason.NotAvailable)]
    [InlineData(-2, AgeSignalsErrorReason.StoreUnavailable)]
    [InlineData(-3, AgeSignalsErrorReason.Network)]
    [InlineData(-4, AgeSignalsErrorReason.StoreUnavailable)]
    [InlineData(-5, AgeSignalsErrorReason.Transient)]
    [InlineData(-6, AgeSignalsErrorReason.StoreUnavailable)]
    [InlineData(-7, AgeSignalsErrorReason.StoreUnavailable)]
    [InlineData(-8, AgeSignalsErrorReason.Transient)]
    [InlineData(-9, AgeSignalsErrorReason.AppNotOwned)]
    [InlineData(-10, AgeSignalsErrorReason.StoreUnavailable)]
    [InlineData(-100, AgeSignalsErrorReason.Unknown)]
    public void Play_Errors(int code, AgeSignalsErrorReason reason)
    {
        var ex = AgeSignalMapper.FromPlayError(code, null);
        Assert.Equal(reason, ex.Reason);
        Assert.Equal(code, ex.NativeCode);
    }
}

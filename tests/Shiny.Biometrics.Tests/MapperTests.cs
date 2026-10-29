using Shiny.Biometrics.Infrastructure;
using Xunit;

namespace Shiny.Biometrics.Tests;


public class MapperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_RejectsEmptyReason(string? reason)
        => Assert.Throws<ArgumentException>(() => BiometricMapper.Validate(new BiometricRequest { Reason = reason! }));


    [Fact]
    public async Task NotSupported_ReportsNothing()
    {
        var biometrics = new NotSupportedBiometrics();
        var availability = await biometrics.GetAvailability();
        Assert.False(availability.IsAvailable);
        Assert.Equal(BiometricStatus.NotSupported, availability.Status);

        var result = await biometrics.Authenticate(new BiometricRequest { Reason = "Unlock" });
        Assert.Equal(BiometricResultStatus.NotAvailable, result.Status);
        Assert.False(result.IsSuccess);
    }


    [Theory]
    [InlineData(0, BiometricType.None)]
    [InlineData(1, BiometricType.Fingerprint)]
    [InlineData(2, BiometricType.Face)]
    [InlineData(4, BiometricType.Iris)]
    [InlineData(8, BiometricType.Unknown)]
    public void Apple_BiometryType(long native, BiometricType expected)
        => Assert.Equal(expected, BiometricMapper.FromAppleBiometryType(native));


    [Theory]
    [InlineData(-7, BiometricStatus.NotEnrolled)]
    [InlineData(-8, BiometricStatus.LockedOut)]
    [InlineData(-5, BiometricStatus.PasscodeNotSet)]
    [InlineData(-6, BiometricStatus.NotSupported)]
    [InlineData(-13, BiometricStatus.Unavailable)]
    public void Apple_Availability(long code, BiometricStatus expected)
        => Assert.Equal(expected, BiometricMapper.FromAppleAvailabilityError(code));


    [Theory]
    [InlineData(-2, BiometricResultStatus.Cancelled)]
    [InlineData(-4, BiometricResultStatus.Cancelled)]
    [InlineData(-9, BiometricResultStatus.Cancelled)]
    [InlineData(-3, BiometricResultStatus.Fallback)]
    [InlineData(-1, BiometricResultStatus.Failed)]
    [InlineData(-8, BiometricResultStatus.LockedOut)]
    [InlineData(-7, BiometricResultStatus.NotEnrolled)]
    [InlineData(-5, BiometricResultStatus.PasscodeNotSet)]
    [InlineData(-6, BiometricResultStatus.NotAvailable)]
    [InlineData(-1004, BiometricResultStatus.NotAvailable)]
    public void Apple_Error(long code, BiometricResultStatus expected)
        => Assert.Equal(expected, BiometricMapper.FromAppleError(code));


    [Theory]
    [InlineData(BiometricStrength.Strong, false, 34, 0x000F)]
    [InlineData(BiometricStrength.Weak, false, 34, 0x00FF)]
    [InlineData(BiometricStrength.Strong, true, 34, 0x800F)]
    [InlineData(BiometricStrength.Weak, true, 34, 0x80FF)]
    [InlineData(BiometricStrength.Strong, true, 29, 0x80FF)]
    [InlineData(BiometricStrength.Strong, true, 28, 0x80FF)]
    [InlineData(BiometricStrength.Strong, true, 27, 0x800F)]
    [InlineData(BiometricStrength.Strong, false, 28, 0x000F)]
    public void Android_Authenticators(BiometricStrength strength, bool allowCredential, int sdk, int expected)
        => Assert.Equal(expected, BiometricMapper.ToAndroidAuthenticators(strength, allowCredential, sdk));


    [Theory]
    [InlineData(0, BiometricStatus.Available)]
    [InlineData(11, BiometricStatus.NotEnrolled)]
    [InlineData(1, BiometricStatus.Unavailable)]
    [InlineData(15, BiometricStatus.SecurityUpdateRequired)]
    [InlineData(12, BiometricStatus.NotSupported)]
    [InlineData(-2, BiometricStatus.NotSupported)]
    [InlineData(-1, BiometricStatus.NotSupported)]
    public void Android_CanAuthenticate(int code, BiometricStatus expected)
        => Assert.Equal(expected, BiometricMapper.FromAndroidCanAuthenticate(code));


    [Theory]
    [InlineData(5, BiometricResultStatus.Cancelled)]
    [InlineData(10, BiometricResultStatus.Cancelled)]
    [InlineData(13, BiometricResultStatus.Cancelled)]
    [InlineData(3, BiometricResultStatus.Timeout)]
    [InlineData(7, BiometricResultStatus.LockedOut)]
    [InlineData(9, BiometricResultStatus.LockedOut)]
    [InlineData(11, BiometricResultStatus.NotEnrolled)]
    [InlineData(14, BiometricResultStatus.PasscodeNotSet)]
    [InlineData(2, BiometricResultStatus.Failed)]
    [InlineData(1, BiometricResultStatus.NotAvailable)]
    [InlineData(12, BiometricResultStatus.NotAvailable)]
    public void Android_Error(int code, BiometricResultStatus expected)
        => Assert.Equal(expected, BiometricMapper.FromAndroidError(code));


    [Theory]
    [InlineData(0, BiometricStatus.Available)]
    [InlineData(1, BiometricStatus.NotSupported)]
    [InlineData(2, BiometricStatus.NotEnrolled)]
    [InlineData(3, BiometricStatus.Unavailable)]
    [InlineData(4, BiometricStatus.Unavailable)]
    public void Windows_Availability(int code, BiometricStatus expected)
        => Assert.Equal(expected, BiometricMapper.FromWindowsAvailability(code));


    [Theory]
    [InlineData(0, BiometricResultStatus.Success)]
    [InlineData(6, BiometricResultStatus.Cancelled)]
    [InlineData(5, BiometricResultStatus.Failed)]
    [InlineData(2, BiometricResultStatus.NotEnrolled)]
    [InlineData(1, BiometricResultStatus.NotAvailable)]
    [InlineData(3, BiometricResultStatus.NotAvailable)]
    [InlineData(4, BiometricResultStatus.NotAvailable)]
    public void Windows_Result(int code, BiometricResultStatus expected)
        => Assert.Equal(expected, BiometricMapper.FromWindowsResult(code));
}

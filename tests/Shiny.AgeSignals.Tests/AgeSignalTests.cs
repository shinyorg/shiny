using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Shiny.AgeSignals.Tests;


public class AgeSignalTests
{
    [Theory]
    // Play default bands / Apple gates 13, 16, 18
    [InlineData(0, 12, 13, false)]
    [InlineData(13, 15, 13, true)]
    [InlineData(13, 15, 16, false)]
    [InlineData(16, 17, 18, false)]
    [InlineData(18, null, 18, true)]
    [InlineData(18, null, 21, null)]       // 18+ straddles 21
    [InlineData(13, 15, 14, null)]         // the band straddles 14
    [InlineData(null, 12, 13, false)]      // Apple leaves the lowest band's lower bound nil
    [InlineData(null, 12, 10, null)]
    public void IsAtLeast(int? lower, int? upper, int age, bool? expected)
        => Assert.Equal(expected, new AgeSignal(AgeSignalStatus.Shared, lower, upper).IsAtLeast(age));


    [Theory]
    [InlineData(AgeSignalStatus.NotSupported)]
    [InlineData(AgeSignalStatus.Declined)]
    [InlineData(AgeSignalStatus.VerificationRequired)]
    public void IsAtLeast_NotShared_IsNull(AgeSignalStatus status)
    {
        var signal = new AgeSignal(status);
        Assert.Null(signal.IsAtLeast(13));
        Assert.Null(signal.IsAdult);
    }


    [Fact]
    public void IsAtLeast_SharedWithoutBounds_IsNull()
        => Assert.Null(new AgeSignal(AgeSignalStatus.Shared).IsAdult);


    [Fact]
    public void Source_IsOrderedWeakestFirst()
    {
        Assert.True(AgeRangeSource.IdVerified > AgeRangeSource.Confirmed);
        Assert.True(AgeRangeSource.Confirmed > AgeRangeSource.GuardianDeclared);
        Assert.True(AgeRangeSource.GuardianDeclared > AgeRangeSource.SelfDeclared);
        Assert.True(AgeRangeSource.SelfDeclared > AgeRangeSource.Unknown);
    }


    [Fact]
    public async Task Registration_NotSupportedOffPlatform()
    {
        var services = new ServiceCollection();
        services.AddAgeSignals();

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        var ages = sp.GetRequiredService<IAgeSignals>();
        Assert.IsType<NotSupportedAgeSignals>(ages);

        Assert.False((await ages.GetEligibility()).IsSupported);
        Assert.Equal(AgeSignalStatus.NotSupported, (await ages.RequestAgeRange(13, 16, 18)).Status);
        Assert.False(await ages.ShowSignificantUpdate("New chat feature"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ages.RequestAgeRange(18, 13));
    }
}

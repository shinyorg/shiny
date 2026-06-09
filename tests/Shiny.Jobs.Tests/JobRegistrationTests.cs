using Shiny.Jobs;
using Xunit;

namespace Shiny.Jobs.Tests;


public class JobRegistrationTests
{
    sealed class DummyJob : IJob
    {
        public System.Threading.Tasks.Task Run(System.Threading.CancellationToken cancelToken)
            => System.Threading.Tasks.Task.CompletedTask;
    }


    // -------- Defaults --------

    [Fact]
    public void Defaults_ArePermissive()
    {
        var reg = new JobRegistration(typeof(DummyJob));

        Assert.Equal(typeof(DummyJob), reg.JobType);
        Assert.False(reg.RunOnForeground);
        Assert.Equal(InternetAccess.None, reg.RequiredInternetAccess);
        Assert.False(reg.DeviceCharging);
        Assert.False(reg.BatteryNotLow);
    }


    // -------- Individual fluent mutators --------

    [Fact]
    public void WithForeground_TogglesFlag()
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithForeground();
        Assert.True(reg.RunOnForeground);
    }


    [Fact]
    public void WithForeground_ExplicitValueWins()
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithForeground(false);
        Assert.False(reg.RunOnForeground);
    }


    [Fact]
    public void WithInternet_SetsRequirement()
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithInternet(InternetAccess.Any);
        Assert.Equal(InternetAccess.Any, reg.RequiredInternetAccess);
    }


    [Fact]
    public void WithCharging_TogglesFlag()
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithCharging();
        Assert.True(reg.DeviceCharging);
    }


    [Fact]
    public void WithBatteryNotLow_TogglesFlag()
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithBatteryNotLow();
        Assert.True(reg.BatteryNotLow);
    }


    // -------- Record semantics --------

    [Fact]
    public void Mutators_ReturnNewInstance_OriginalUnchanged()
    {
        var original = new JobRegistration(typeof(DummyJob));
        var modified = original.WithForeground();

        Assert.False(original.RunOnForeground);
        Assert.True(modified.RunOnForeground);
        Assert.NotSame(original, modified);
    }


    [Fact]
    public void Chained_Mutators_AccumulateAcrossAllFields()
    {
        var reg = new JobRegistration(typeof(DummyJob))
            .WithForeground()
            .WithInternet(InternetAccess.Unmetered)
            .WithCharging()
            .WithBatteryNotLow();

        Assert.True(reg.RunOnForeground);
        Assert.Equal(InternetAccess.Unmetered, reg.RequiredInternetAccess);
        Assert.True(reg.DeviceCharging);
        Assert.True(reg.BatteryNotLow);
        Assert.Equal(typeof(DummyJob), reg.JobType);
    }


    [Fact]
    public void Equality_ByValue()
    {
        var a = new JobRegistration(typeof(DummyJob)).WithForeground().WithCharging();
        var b = new JobRegistration(typeof(DummyJob)).WithForeground().WithCharging();
        Assert.Equal(a, b);
    }
}

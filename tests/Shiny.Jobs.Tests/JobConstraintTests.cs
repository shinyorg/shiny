using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Jobs.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Xunit;

namespace Shiny.Jobs.Tests;


public class JobConstraintTests
{
    sealed class DummyJob : IJob
    {
        public Task Run(CancellationToken cancelToken) => Task.CompletedTask;
    }

    sealed class NetJob : IJob
    {
        public Task Run(CancellationToken cancelToken) => Task.CompletedTask;
    }

    sealed class UnmeteredJob : IJob
    {
        public Task Run(CancellationToken cancelToken) => Task.CompletedTask;
    }

    sealed class PowerUnmeteredJob : IJob
    {
        public Task Run(CancellationToken cancelToken) => Task.CompletedTask;
    }


    sealed class FakeBattery(double level, BatteryState status) : IBattery
    {
        public event EventHandler? Changed { add { } remove { } }
        public BatteryState Status => status;
        public double Level => level;
        public BatteryPowerSource PowerSource => BatteryPowerSource.Unknown;
        public EnergySaverStatus EnergySaverStatus => EnergySaverStatus.Unknown;
    }


    sealed class FakeConnectivity(Net.NetworkAccess access, ConnectionTypes types) : IConnectivity
    {
        public event EventHandler? Changed { add { } remove { } }
        public ConnectionTypes ConnectionTypes => types;
        public Net.NetworkAccess Access => access;
    }


    static readonly JobRegistration BatteryNotLow = new JobRegistration(typeof(DummyJob)).WithBatteryNotLow();


    [Theory]
    [InlineData(0.9, true)]
    [InlineData(0.21, true)]
    [InlineData(0.2, false)]
    [InlineData(0.05, false)]
    public void BatteryNotLow_UsesFractionalLevel(double level, bool expected)
        => Assert.Equal(expected, JobConstraints.HasPowerLevel(BatteryNotLow, new FakeBattery(level, BatteryState.Discharging)));


    [Fact]
    public void BatteryNotLow_LowButCharging_Runs()
        => Assert.True(JobConstraints.HasPowerLevel(BatteryNotLow, new FakeBattery(0.05, BatteryState.Charging)));


    [Fact]
    public void NoBatteryApi_TreatedAsMainsPowered()
    {
        Assert.True(JobConstraints.HasPowerLevel(BatteryNotLow, null));
        Assert.True(JobConstraints.HasChargeStatus(new JobRegistration(typeof(DummyJob)).WithCharging(), null));
    }


    [Fact]
    public void Charging_RequiresExternalPower()
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithCharging();
        Assert.False(JobConstraints.HasChargeStatus(reg, new FakeBattery(0.5, BatteryState.Discharging)));
        Assert.True(JobConstraints.HasChargeStatus(reg, new FakeBattery(0.5, BatteryState.Charging)));
    }


    [Theory]
    [InlineData(Net.NetworkAccess.Internet, ConnectionTypes.Wifi, true)]
    [InlineData(Net.NetworkAccess.Internet, ConnectionTypes.Cellular, false)]
    [InlineData(Net.NetworkAccess.Internet, ConnectionTypes.Wifi | ConnectionTypes.Cellular, false)]
    [InlineData(Net.NetworkAccess.ConstrainedInternet, ConnectionTypes.Wifi, false)]
    [InlineData(Net.NetworkAccess.None, ConnectionTypes.None, false)]
    public void Unmetered_ExcludesCellularAndConstrained(Net.NetworkAccess access, ConnectionTypes types, bool expected)
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithInternet(InternetAccess.Unmetered);
        Assert.Equal(expected, JobConstraints.HasReqInternet(reg, new FakeConnectivity(access, types)));
    }


    [Theory]
    [InlineData(Net.NetworkAccess.Internet, ConnectionTypes.Cellular, true)]
    [InlineData(Net.NetworkAccess.ConstrainedInternet, ConnectionTypes.Cellular, true)]
    [InlineData(Net.NetworkAccess.Local, ConnectionTypes.Wifi, false)]
    public void AnyInternet_AllowsCellular(Net.NetworkAccess access, ConnectionTypes types, bool expected)
    {
        var reg = new JobRegistration(typeof(DummyJob)).WithInternet(InternetAccess.Any);
        Assert.Equal(expected, JobConstraints.HasReqInternet(reg, new FakeConnectivity(access, types)));
    }


    static JobManager CreateManager()
    {
        var services = new ServiceCollection();
        var registrar = new JobRegistrar(services);
        registrar.Register<NetJob>(r => r.WithInternet(InternetAccess.Any));
        registrar.Register<UnmeteredJob>(r => r.WithInternet(InternetAccess.Unmetered));
        registrar.Register<PowerUnmeteredJob>(r => r.WithInternet(InternetAccess.Unmetered).WithCharging());

        return new JobManager(
            services.BuildServiceProvider(),
            NullLogger<IJobManager>.Instance,
            new FakeConnectivity(Net.NetworkAccess.Internet, ConnectionTypes.Wifi),
            registrar
        );
    }


    [Fact]
    public void Categories_Default_KeepUnmeteredInNet()
    {
        using var mgr = CreateManager();
        var net = mgr.GetJobsByCategory(AbstractJobManager.GetCategoryId(false, true));
        Assert.Equal(2, net.Count);
        Assert.Contains(net, x => x.JobType == typeof(UnmeteredJob));

        Assert.Empty(mgr.GetJobsByCategory(AbstractJobManager.GetUnmeteredCategoryId(false)));
        Assert.Single(mgr.GetJobsByCategory(AbstractJobManager.GetCategoryId(true, true)));
    }


    [Fact]
    public void Categories_SeparateUnmetered_SplitsThemOut()
    {
        using var mgr = CreateManager();
        var net = mgr.GetJobsByCategory(AbstractJobManager.GetCategoryId(false, true), true);
        Assert.Single(net);
        Assert.Equal(typeof(NetJob), net[0].JobType);

        var unmetered = mgr.GetJobsByCategory(AbstractJobManager.GetUnmeteredCategoryId(false), true);
        Assert.Single(unmetered);
        Assert.Equal(typeof(UnmeteredJob), unmetered[0].JobType);

        Assert.Empty(mgr.GetJobsByCategory(AbstractJobManager.GetCategoryId(true, true), true));
        var powerUnmetered = mgr.GetJobsByCategory(AbstractJobManager.GetUnmeteredCategoryId(true), true);
        Assert.Single(powerUnmetered);
        Assert.Equal(typeof(PowerUnmeteredJob), powerUnmetered[0].JobType);
    }


    [Fact]
    public void JobManager_ResolvesWithoutBattery()
    {
        // plain .NET on Windows / macOS registers no IBattery
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJob<NetJob>();
        services.RemoveAll<IBattery>();

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IJobManager>());
    }
}

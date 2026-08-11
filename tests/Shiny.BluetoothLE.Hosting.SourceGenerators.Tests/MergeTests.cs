using System;
using System.Linq;
using Xunit;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators.Tests;


public class MergeTests
{
    const string TwoClassesOneUuid = """
        [BleService("180D", Name = "HeartRate", Advertise = true)]
        public partial class HeartRateService
        {
            [ReadCharacteristic("2A37")]
            byte[] Read() => new byte[] { 1 };
        }

        [BleService("0000180D-0000-1000-8000-00805F9B34FB")]
        public partial class HeartExtras
        {
            [ReadCharacteristic("2A38")]
            byte[] Read() => new byte[] { 2 };
        }
        """;


    [Fact]
    public void ClassesSharingAUuid_ProduceOneAddServiceCall()
    {
        var run = GeneratorHarness.Run(Snippets.Raw(TwoClassesOneUuid));

        Assert.Empty(run.Ids());
        var registration = run.Source("BleHostedServiceRegistration.g.cs");

        // AddService is keyed by UUID in BleHostingManager, so a second call would throw at runtime
        Assert.Equal(2, Occurrences(registration, "manager.AddService(")); // aggregate + a-la-carte
        Assert.Contains("heartExtras.BuildBleService(builder);", registration);
        Assert.Contains("heartRateService.BuildBleService(builder);", registration);
    }


    [Fact]
    public void MergeGroup_TakesItsNameFromWhicheverMemberSetOne()
    {
        var run = GeneratorHarness.Run(Snippets.Raw(TwoClassesOneUuid));

        Assert.Empty(run.Ids());
        // HeartExtras sorts first alphabetically, but HeartRateService is the one that named the group
        Assert.Contains("AddHeartRate(this", run.Source("BleHostedServiceRegistration.g.cs"));
    }


    [Fact]
    public void Advertise_OnAnyMember_AdvertisesTheMergedUuidOnce()
    {
        var run = GeneratorHarness.Run(Snippets.Raw(TwoClassesOneUuid));

        Assert.Empty(run.Ids());
        var registration = run.Source("BleHostedServiceRegistration.g.cs");
        Assert.Contains("StartBleHostedAdvertising", registration);
        Assert.Equal(1, Occurrences(registration, "AdvertisementOptions(localName, \"0000180D-0000-1000-8000-00805F9B34FB\")"));
    }


    [Fact]
    public void NoAdvertisedService_OmitsTheAdvertisingHelper()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            """));

        Assert.Empty(run.Ids());
        Assert.DoesNotContain("StartBleHostedAdvertising", run.AllSource);
    }


    [Fact]
    public void DependencyInjection_RegistersEveryHostedServiceAsASingleton()
    {
        var run = GeneratorHarness.Run(Snippets.Raw(TwoClassesOneUuid));

        Assert.Empty(run.Ids());
        var registration = run.Source("BleHostedServiceRegistration.g.cs");
        Assert.Contains("AddSingleton<global::TestApp.HeartRateService>(services)", registration);
        Assert.Contains("AddSingleton<global::TestApp.HeartExtras>(services)", registration);
        Assert.Contains("AttachBleHostedServices(this global::Shiny.BluetoothLE.Hosting.IBleHostingManager manager, global::System.IServiceProvider services)", registration);
    }


    [Fact]
    public void RegistrationClass_LandsInTheProjectsRootNamespace()
    {
        var run = GeneratorHarness.Run(Snippets.Raw(TwoClassesOneUuid), rootNamespace: "My.App");

        Assert.Empty(run.Ids());
        Assert.Contains("namespace My.App", run.Source("BleHostedServiceRegistration.g.cs"));
    }


    [Fact]
    public void NoHostedServices_GeneratesNothing()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("public partial class Nothing { }"));

        Assert.Empty(run.Ids());
        Assert.Empty(run.Sources);
    }


    static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}

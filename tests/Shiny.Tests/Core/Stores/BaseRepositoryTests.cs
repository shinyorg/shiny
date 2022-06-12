using FluentAssertions;
using Shiny.Beacons;
using Shiny.Beacons.Infrastructure;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;
using Shiny.Locations;
using Shiny.Locations.Infrastructure;
using Shiny.Net.Http;
using Shiny.Notifications;
using Shiny.Notifications.Infrastructure;
using Shiny.Stores;
using Xunit;

namespace Shiny.Tests.Core.Stores;


[Trait("Category", "Repository")]
public abstract class BaseRepositoryTests
{
    protected abstract IRepository<TModel> Create<TModel, TConverter>()
        where TModel : IStoreEntity
        where TConverter : class, IStoreConverter<TModel>, new();


    [Fact(DisplayName = "Repository - Persist")]
    public async Task PersistTests()
    {
        var repo1 = this.Create<TestModel, TestModelStore>();
        var num = new Random().Next(1, 9999);

        await repo1.Set(new TestModel
        {
            Identifier = "value1",
            IntValue = num
        });

        var repo2 = this.Create<TestModel, TestModelStore>();
        var obj = await repo2.Get("value1");

        obj.Should().NotBeNull();
        obj.Identifier.Should().Be("value1");
        obj.IntValue.Should().Be(num);
    }


    [Fact(DisplayName = "Repository - Update")]
    public async Task UpdateTest()
    {
        var repo = this.Create<TestModel, TestModelStore>();
        await repo.Set(new TestModel
        {
            Identifier = "1",
            IntValue = 1
        });
        await repo.Set(new TestModel
        {
            Identifier = "1",
            IntValue = 2
        });
        var results = await repo.GetList();
        results.Count.Should().Be(1);
        results.First().IntValue.Should().Be(2);
    }


    [Fact(DisplayName = "Repository - GetList")]
    public async Task MultipleModels()
    {
        var repo = this.Create<TestModel, TestModelStore>();
        await repo.Set(new TestModel { Identifier = "1" });
        await repo.Set(new TestModel { Identifier = "2" });

        var all = await repo.GetList();
        all.Count.Should().Be(2);

        all.First(x => x.Identifier.Equals("1")).Should().NotBeNull();
        all.First(x => x.Identifier.Equals("2")).Should().NotBeNull();
    }


    [Fact(DisplayName = "Repository - Remove")]
    public async Task RemoveTest()
    {
        var repo1 = this.Create<TestModel, TestModelStore>();
        await repo1.Set(new TestModel { Identifier = "1" });
        await repo1.Remove("1");

        var repo2 = this.Create<TestModel, TestModelStore>();
        var result = await repo2.Get("1");
        result.Should().BeNull();
    }


    [Fact(DisplayName = "Repository - Clear")]
    public async Task ClearTest()
    {
        await this.MultipleModels();
        var repo = this.Create<TestModel, TestModelStore>();
        await repo.Clear();

        var r = await repo.GetList();
        r.Count.Should().Be(0);
    }


    [Fact(DisplayName = "Repository - Types - Notifications")]
    public async Task NotificationPersist()
    {
        var repo = this.Create<Notification, NotificationStoreConverter>();
    }


    [Fact(DisplayName = "Repository - Types - Beacon Regions")]
    public async Task BeaconPersist()
    {
        var repo = this.Create<BeaconRegion, BeaconRegionStoreConverter>();
        var uuid = Guid.NewGuid();

        await repo.Set(new BeaconRegion(
            "test",
            uuid,
            10,
            11
        ));

        var region = await repo.Get("test");
        region.Identifier.Should().Be("test");
        region.Uuid.Should().Be(uuid);
        region.Major.Should().Be(10);
        region.Minor.Should().Be(11);
    }


    [Fact(DisplayName = "Repository - Types - Geofence Regions")]
    public async Task GeofencePersist()
    {
        var repo = this.Create<GeofenceRegion, GeofenceRegionStoreConverter>();
        await repo.Set(new GeofenceRegion(
            "geotest",
            new Position(1, 1),
            Distance.FromMeters(300)
        ));

        var region = await repo.Get("geotest");
        region.Should().NotBeNull();
    }


    [Fact(DisplayName = "Repository - Types - Channels")]
    public async Task ChannelPersist()
    {
        var repo = this.Create<Channel, ChannelStoreConverter>();
        await repo.Set(new Channel
        {
            Identifier = "test",
            Description = "channel description",
            Importance = ChannelImportance.Low,
            CustomSoundPath = "sound path",
            Actions =
            {
                ChannelAction.Create("", "", ChannelActionType.OpenApp)
            }
        });

        var channel = await repo.Get("test");
        channel.Should().NotBeNull("Channel should not be null");
    }


    [Fact(DisplayName = "Repository - Types - Jobs")]
    public async Task JobsPersist()
    {
        var repo = this.Create<JobInfo, JobInfoStoreConverter>();
        await repo.Set(new JobInfo(
            typeof(object),
            "",
            true
        )
        {
            DeviceCharging = true,
            RequiredInternetAccess = InternetAccess.Unmetered
        });
        var job = await repo.Get("");
    }


    [Fact(DisplayName = "Repository - Types - HTTP Transfers")]
    public async Task HttpStorePersist()
    {
        var repo = this.Create<HttpTransfer, HttpTransferStoreConverter>();
    }
}

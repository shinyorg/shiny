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


    [Fact]
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


    [Fact]
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


    [Fact]
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


    [Fact]
    public async Task RemoveTest()
    {
        var repo1 = this.Create<TestModel, TestModelStore>();
        await repo1.Set(new TestModel { Identifier = "1" });
        await repo1.Remove("1");

        var repo2 = this.Create<TestModel, TestModelStore>();
        var result = await repo2.Get("1");
        result.Should().BeNull();
    }


    [Fact]
    public async Task ClearTest()
    {
        await this.MultipleModels();
        var repo = this.Create<TestModel, TestModelStore>();
        await repo.Clear();

        var r = await repo.GetList();
        r.Count.Should().Be(0);
    }


    [Fact]
    public async Task NotificationPersist()
    {
        var repo = this.Create<Notification, NotificationStoreConverter>();
    }


    [Fact]
    public async Task BeaconPersist()
    {
        var repo = this.Create<BeaconRegion, BeaconRegionStoreConverter>();
    }


    [Fact]
    public async Task GeofencePersist()
    {
        var repo = this.Create<GeofenceRegion, GeofenceRegionStoreConverter>();
    }


    [Fact]
    public async Task ChannelPersist()
    {
        var repo = this.Create<Channel, ChannelStoreConverter>();
        await repo.Set(new Channel
        {
            Identifier = "test"
        });

        var channel = await repo.Get("test");
        channel.Should().NotBeNull("Channel should not be null");
    }


    [Fact]
    public async Task JobsPersist()
    {
        var repo = this.Create<JobInfo, JobInfoStoreConverter>();
    }


    [Fact]
    public async Task HttpStorePersist()
    {
        var repo = this.Create<HttpTransfer, HttpTransferStoreConverter>();
    }
}

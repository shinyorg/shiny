using Shiny.Hosting;
using Shiny.Notifications;

namespace Shiny.Tests.Notifications;


public class NotificationTests : AbstractShinyTests
{
    public NotificationTests(ITestOutputHelper output) : base(output) { }

    INotificationManager Notifications => this.GetService<INotificationManager>();
    protected override void Configure(HostBuilder hostBuilder) => hostBuilder.Services.AddNotifications();
    public override void Dispose()
    {
        //this.Notifications.Clear().GetAwaiter().GetResult();
        this.Notifications.ClearChannels();
        base.Dispose();
    }


    [Fact(DisplayName = "Notifications - Standard")]
    public async Task StandardTest()
    {
        this.CreateFullChannel(nameof(this.StandardTest));
        await this.Notifications.Send("Test", "Test 1", nameof(this.StandardTest));
    }


    [Fact(DisplayName = "Notifications - Channel Store")]
    public void ChannelStoreProperTest()
    {
        var created = this.CreateFullChannel(nameof(this.ChannelStoreProperTest));

        var channel = this.Notifications.GetChannels().Single();

        // TODO: compare
    }


    [Fact(DisplayName = "Notifications - SetChannels")]
    public async Task SetChannels()
    {
        this.CreateChannel("1");
        this.CreateChannel("2");
        this.CreateNotification(1, "1");
        this.CreateNotification(2, "1");
        this.CreateNotification(3, "2");

        this.Notifications.AddChannel(new Channel { Identifier = "1", Description = "1" });
        this.Notifications.AddChannel(new Channel { Identifier = "3", Description = "3" });
        var notifications = await this.Notifications.GetPendingNotifications();

        notifications.First(x => x.Id == 1).Channel.Should().Be("1");
        notifications.First(x => x.Id == 2).Channel.Should().Be("1");
        notifications.First(x => x.Id == 3).Channel.Should().BeNull();

        var channels = this.Notifications.GetChannels();
        channels.Count.Should().Be(2);
        channels.FirstOrDefault(x => x.Identifier == "1").Should().NotBeNull("1 should be found");
        channels.FirstOrDefault(x => x.Identifier == "2").Should().BeNull("2 should NOT be found");
        channels.FirstOrDefault(x => x.Identifier == "3").Should().NotBeNull("3 should be found");
    }


    [Fact(DisplayName = "Notifications - Clear Channels")]
    public async Task ClearChannelsTest()
    {
        this.CreateChannel("1");
        this.CreateChannel("2");
        this.CreateChannel("3");

        await this.CreateNotification(1, "1");
        await this.CreateNotification(2, "2");
        await this.CreateNotification(3, "3");

        this.Notifications.ClearChannels();

        var notifications = await this.Notifications.GetPendingNotifications();
        notifications.Count().Should().Be(3);
        foreach (var notification in notifications)
            notification.Channel.Should().BeNull();
    }


    Channel CreateFullChannel(string identifier)
    {
        var channel = new Channel
        {
            Identifier = identifier
        };
        this.Notifications.AddChannel(channel);
        return channel;
    }


    void CreateChannel(string name) => this.Notifications.AddChannel(new Channel
    {
        Identifier = name,
        Description = name
    });


    Task CreateNotification(int id, string channel)
        => this.Notifications.Send(new Shiny.Notifications.Notification
        {
            Id = id,
            Title = id.ToString(),
            Message = id.ToString(),
            ScheduleDate = DateTime.Now.AddDays(30),
            Channel = channel
        });
}
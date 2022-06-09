using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Shiny.Hosting;
using Shiny.Notifications;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests.Push;


public class NotificationTests : AbstractShinyTests
{
    public NotificationTests(ITestOutputHelper output) : base(output) { }
    
    INotificationManager Notifications => this.GetService<INotificationManager>();
    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddNotifications();
    public override void Dispose()
    {
        //this.Notifications.Clear().GetAwaiter().GetResult();
        this.Notifications.ClearChannels().GetAwaiter().GetResult();
        base.Dispose();
    }


    [Fact(DisplayName = "Notifications - Standard")]
    public async Task StandardTest()
    {
        await this.CreateFullChannel(nameof(StandardTest));
        await this.Notifications.Send("Test", "Test 1", nameof(StandardTest));
    }


    [Fact(DisplayName = "Notifications - Channel Store")]
    public async Task ChannelStoreProperTest()
    {
        var created = await this.CreateFullChannel(nameof(ChannelStoreProperTest));

        var channel = (await this.Notifications.GetChannels()).Single();

        // TODO: compare
    }


    [Fact(DisplayName = "Notifications - SetChannels")]
    public async Task SetChannels()
    {
        await this.CreateChannel("1");
        await this.CreateChannel("2");
        await this.CreateNotification(1, "1");
        await this.CreateNotification(2, "1");
        await this.CreateNotification(3, "2");

        await this.Notifications.AddChannel(new Channel { Identifier = "1", Description = "1" });
        await this.Notifications.AddChannel(new Channel { Identifier = "3", Description = "3" });
        var notifications = await this.Notifications.GetPendingNotifications();

        notifications.First(x => x.Id == 1).Channel.Should().Be("1");
        notifications.First(x => x.Id == 2).Channel.Should().Be("1");
        notifications.First(x => x.Id == 3).Channel.Should().BeNull();

        var channels = await this.Notifications.GetChannels();
        channels.Count.Should().Be(2);
        channels.FirstOrDefault(x => x.Identifier == "1").Should().NotBeNull("1 should be found");
        channels.FirstOrDefault(x => x.Identifier == "2").Should().BeNull("2 should NOT be found");
        channels.FirstOrDefault(x => x.Identifier == "3").Should().NotBeNull("3 should be found");
    }


    [Fact(DisplayName = "Notifications - Clear Channels")]
    public async Task ClearChannelsTest()
    {
        await this.CreateChannel("1");
        await this.CreateChannel("2");
        await this.CreateChannel("3");

        await this.CreateNotification(1, "1");
        await this.CreateNotification(2, "2");
        await this.CreateNotification(3, "3");

        await this.Notifications.ClearChannels();

        var notifications = await this.Notifications.GetPendingNotifications();
        notifications.Count().Should().Be(3);
        foreach (var notification in notifications)
            notification.Channel.Should().BeNull();
    }


    async Task<Channel> CreateFullChannel(string identifier)
    {
        var channel = new Channel
        {
            Identifier = identifier
        };
        await this.Notifications.AddChannel(channel);
        return channel;
    }


    Task CreateChannel(string name) => this.Notifications.AddChannel(new Channel
    {
        Identifier = name,
        Description = name
    });


    Task CreateNotification(int id, string channel)
        => this.Notifications.Send(new Notification
        {
            Id = id,
            Title = id.ToString(),
            Message = id.ToString(),
            ScheduleDate = DateTime.Now.AddDays(30),
            Channel = channel
        });
}
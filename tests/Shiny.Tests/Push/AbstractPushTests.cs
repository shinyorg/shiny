using Shiny.Push;

namespace Shiny.Tests.Push;


public abstract class AbstractPushTests : AbstractShinyTests
{
    protected AbstractPushTests(ITestOutputHelper output) : base(output) {}


    protected IPushManager Manager => this.GetService<IPushManager>();
    protected abstract Task Send(string title, string message, params (string Key, string Value)[] parameters);


    public async override void Dispose()
    {
        // TODO: need async dispose
        await this.Manager.UnRegister();
    }


    [Fact]
    public async Task EndToEnd()
    {
        (await this.Manager.RequestAccess()).Assert();
        var task = this.Manager
            .WhenReceived()
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();

        await this.Send("Test Title", "Test Body");
        //var response = await this.apnSender.SendAsync(
        //    new AppleNotification
        //    {
        //        AlertBody = new AppleNotification.Alert
        //        {
        //            Title = "Test Title",
        //            Body = "Test Body"
        //        }
        //    },
        //    access.RegistrationToken
        //);
        //response.IsSuccess.Should().BeTrue();

        var result = await task.ConfigureAwait(false);
        result.Notification.Should().NotBeNull("Notification is null");
        result.Notification!.Title.Should().Be("Test Title");
        result.Notification!.Message.Should().Be("Test Body");
    }
}

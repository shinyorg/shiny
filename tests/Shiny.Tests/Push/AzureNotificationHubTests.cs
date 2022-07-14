using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Shiny.Hosting;
using Shiny.Push;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests.Push;


public class AzureNotificationHubTests : AbstractPushTests
{
    private const string FcmSampleNotificationContent = "{\"data\":{\"message\":\"Notification Hub test notification from SDK sample\"}}";
    private const string FcmSampleSilentNotificationContent = "{ \"message\":{\"data\":{ \"Nick\": \"Mario\", \"body\": \"great match!\", \"Room\": \"PortugalVSDenmark\" } }}";
    private const string AppleSampleNotificationContent = "{\"aps\":{\"alert\":\"Notification Hub test notification from SDK sample\",\"content-available\":1}}";
    private const string AppleSampleSilentNotificationContent = "{\"aps\":{\"content-available\":1}, \"foo\": 2 }";
    readonly IPushTagSupport pushManager;
    readonly NotificationHubClient hubClient;

    public AzureNotificationHubTests(ITestOutputHelper output) : base(output) {}
    protected override void Configure(IHostBuilder hostBuilder)
    {
        //        BuildServices = x => x.UsePushAzureNotificationHubs<TestPushDelegate>(
        //            Secrets.Values.AzureNotificationHubListenerConnectionString,
        //            Secrets.Values.AzureNotificationHubName
        //        ),
        //    this.hubClient = NotificationHubClient.CreateClientFromConnectionString(
        //        Secrets.Values.AzureNotificationHubFullConnectionString,
        //        Secrets.Values.AzureNotificationHubName
        //    );
        //    this.pushManager = (IPushTagSupport)ShinyHost.Resolve<IPushManager>();
    }


    // TODO
    protected override Task Send(string title, string message, params (string Key, string Value)[] parameters) => Task.CompletedTask;


    [Fact(DisplayName = "Push - ANH - Register/UnRegister")]
    public Task Register_UnRegister() => this.WrapRegistration(async regToken =>
    {
        this.pushManager.CurrentRegistrationToken.Should().Be(regToken);
        this.pushManager.CurrentRegistrationTokenDate.Should().NotBeNull("TokenDate not set");

        // did it remove off the server?
        var install = await this.hubClient.GetInstallationAsync(regToken);
        install.Should().NotBeNull("Install was not found");

        await this.pushManager.UnRegister();
        this.pushManager.CurrentRegistrationToken.Should().BeNull("Reg Token is still set");
        this.pushManager.CurrentRegistrationTokenDate.Should().BeNull("Reg Token Date is still set");
        await Task.Delay(1000); // breath azure - gives things enough time to ensure azure has cleaned up

        try
        {
            await this.hubClient.GetInstallationAsync(regToken);
            throw new ArgumentException("This should not have been reached");
        }
        catch (MessagingEntityNotFoundException)
        {
            //this is what we want
        }
    });


    [Fact(DisplayName = "Push - ANH - Receive Observable")]
    public Task ReceiveOnForegroundObservable() => this.WrapRegistration(async token =>
    {
        var task = this.pushManager
            .WhenReceived()
            .Take(1)
            .ToTask();

        await this.DoSend();

        await task.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);
    });


    [Fact(DisplayName = "Push - ANH - Tag Expression")]
    public Task TagExpression() => this.WrapRegistration(async token =>
    {
        await this.pushManager.SetTags("TagExpression", token);
        var task = this.pushManager
            .WhenReceived()
            .Take(1)
            .ToTask();

        await this.DoSend("TagExpression && " + token);
        await task.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);
    });


    [Fact(DisplayName = "Push - ANH - Receive Delegate")]
    public Task ReceiveOnDelegate() => this.WrapRegistration(async token =>
    {
        var tcs = new TaskCompletionSource<object?>();
        //TestPushDelegate.Receive += _ => tcs.SetResult(null);

        await this.DoSend();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);
    });



    [Fact(DisplayName = "Push - ANH - Tags")]
    public Task Tags() => this.WrapRegistration(async token =>
    {
        this.Log("Setting 3 random tags");
        var tag1 = CreateRandomTag("1-");
        var tag2 = CreateRandomTag("2-");
        var tag3 = CreateRandomTag("3-");
        await this.pushManager.SetTags(tag1, tag2, tag3);
        this.Log($"Tags: {tag1}, {tag2}, {tag3}");

        await Task.Delay(1000); // it's like azure takes a second to propagate even though it has returned all is good
        var install = await this.hubClient.GetInstallationAsync(token);
        install.Tags.Any(x => x.Equals(tag1, StringComparison.InvariantCultureIgnoreCase)).Should().BeTrue("tag1 not found");
        install.Tags.Any(x => x.Equals(tag2, StringComparison.InvariantCultureIgnoreCase)).Should().BeTrue("tag2 not found");
        install.Tags.Any(x => x.Equals(tag3, StringComparison.InvariantCultureIgnoreCase)).Should().BeTrue("tag3 not found");
        this.Log("Tags verified with Azure");

        await this.pushManager.RemoveTag(tag2);
        await Task.Delay(1000);
        install = await this.hubClient.GetInstallationAsync(token);

        install.Tags.Any(x => x.Equals(tag1, StringComparison.InvariantCultureIgnoreCase)).Should().BeTrue("tag1 not found");
        install.Tags.Any(x => x.Equals(tag2, StringComparison.InvariantCultureIgnoreCase)).Should().BeFalse("tag2 found, but should be deleted");
        install.Tags.Any(x => x.Equals(tag3, StringComparison.InvariantCultureIgnoreCase)).Should().BeTrue("tag3 not found");
        this.Log("Tags verified with Azure after remove");

        this.Log("Verifying send to tag");
        var task = this.pushManager
            .WhenReceived()
            .Take(1)
            .ToTask();
        await this.DoSend(tag1);

        await task.WaitAsync(TimeSpan.FromSeconds(20), CancellationToken.None);
        this.Log("Tag send verified");

        await this.pushManager.ClearTags();
        await Task.Delay(1000);
        install = await this.hubClient.GetInstallationAsync(token);
        install.Tags.Should().BeNull("There should be 0 tags on this install now");
    });


    static string CreateRandomTag(string prefix)
        => prefix + Guid.NewGuid().ToString().Replace("-", "").ToLower().Substring(0, 8);


    async Task WrapRegistration(Func<string, Task> innerTask)
    {
        var result = await this.pushManager.RequestAccess();
        result.Assert();

        try
        {
            await Task.Delay(1000); // azure needs a sec
            await innerTask(result.RegistrationToken!);
        }
        finally
        {
            await this.pushManager.UnRegister();
        }
    }


    async Task DoSend(string? tag = null)
    {
        if (tag == null)
        {
            tag = "UnitTest";
            await this.pushManager.AddTag(tag);
        }
        await Task.Delay(1000); // let azure breath

        var regs = await this.hubClient.GetRegistrationsByTagAsync(tag, 5);
        regs.Count().Should().Be(1, "Invalid Registration Count");
        this.Log("Using push token " + tag);

        // TODO
        //var p = TestStartup.CurrentPlatform;

        //if (p.IsAndroid())
        //{
        //    await this.hubClient.SendFcmNativeNotificationAsync(FcmSampleNotificationContent, tag);
        //}
        //else if (p.IsIos())
        //{
        //    await this.hubClient.SendAppleNativeNotificationAsync(AppleSampleNotificationContent, tag);
        //}
        //else if (p.IsUwp())
        //{
        //    await this.hubClient.SendWindowsNativeNotificationAsync(WnsSampleNotification, tag);
        //}
        //else
        //{
        //    throw new ArgumentException("Invalid Platform - " + p.Name);
        //}
    }
}
using System;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis;

namespace Shiny.Generators.Tests
{
    public class iOSAppDelegateSourceGeneratorTests : AbstractSourceGeneratorTests<iOSAppDelegateSourceGenerator>
    {
        // TODO: build xam ios libs, add appdelegate
        // test for finding custom shiny startup
        // test with/out XF
        // test for auto-init of 3rd party
        const string StandardAppDelegateClassName = "MyTest.TestAppDelegate";
        const string StandardAppDelegate = @"
[assembly: Shiny.ShinyApplicationAttribute]
namespace MyTest
{
    public partial class TestAppDelegate : UIKit.UIApplicationDelegate
    {
    }
}";
        public iOSAppDelegateSourceGeneratorTests(ITestOutputHelper output) : base(output, "Xamarin.iOS", "Shiny", "Shiny.Core") { }


        [Fact]
        public void PerformFetchGenerated()
        {
            this.Generator.AddSource(StandardAppDelegate);
            this.RunGenerator();
            this.GetDefaultAppDelegate().HasMethod("PerformFetch").Should().BeTrue("PerformFetch was not generated");
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HandleEventsForBackgroundUrlGeneration(bool includeHttpTransfers)
        {
            if (includeHttpTransfers)
                this.Generator.AddReference("Shiny.Net.Http");

            this.Generator.AddSource(StandardAppDelegate);
            this.RunGenerator();
            this.GetDefaultAppDelegate()
                .HasMethod("HandleEventsForBackgroundUrl")
                .Should()
                .Be(includeHttpTransfers);
        }


        [Theory]
        [InlineData("Shiny.Push")]
        [InlineData("Shiny.Push.AzureNotificationHubs")]
        [InlineData("Shiny.Push.FirebaseMessaging")]
        [InlineData("Shiny.Push.OneSignal")]
        public void PushEventsGenerated(string libraryToInclude)
        {
            this.Generator.AddReference(libraryToInclude);
            this.Generator.AddReference("Shiny.Push.Abstractions");
            this.Generator.AddSource(StandardAppDelegate);
            this.RunGenerator();

            var appDelegate = this.GetDefaultAppDelegate();
            appDelegate.HasMethod("ReceivedRemoteNotification").Should().BeTrue("ReceivedRemoteNotification");
            appDelegate.HasMethod("DidReceiveRemoteNotification").Should().BeTrue("DidReceiveRemoteNotification");
            appDelegate.HasMethod("RegisteredForRemoteNotifications").Should().BeTrue("RegisteredForRemoteNotifications");
            appDelegate.HasMethod("FailedToRegisterForRemoteNotifications").Should().BeTrue("FailedToRegisterForRemoteNotifications");
        }


        [Fact]
        public void NoPushEventsGenerated()
        {
            this.Generator.AddReference("Shiny.Push.Abstractions");
            this.Generator.AddSource(StandardAppDelegate);
            this.RunGenerator();

            var appDelegate = this.GetDefaultAppDelegate();
            appDelegate.HasMethod("ReceivedRemoteNotification").Should().BeFalse("ReceivedRemoteNotification");
            appDelegate.HasMethod("DidReceiveRemoteNotification").Should().BeFalse("DidReceiveRemoteNotification");
            appDelegate.HasMethod("RegisteredForRemoteNotifications").Should().BeFalse("RegisteredForRemoteNotifications");
            appDelegate.HasMethod("FailedToRegisterForRemoteNotifications").Should().BeFalse("FailedToRegisterForRemoteNotifications");
        }


        INamedTypeSymbol GetDefaultAppDelegate()
        {
            var appDelegate = this.Compilation.GetTypeByMetadataName(StandardAppDelegateClassName);
            appDelegate.Should().NotBeNull("AppDelegate type not found");
            return appDelegate;
        }
    }
}

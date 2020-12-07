using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tests
{
    public class iOSAppDelegateSourceGeneratorTests : AbstractSourceGeneratorTests<iOSAppDelegateSourceGenerator>
    {
        // TODO: test for auto-init of 3rd party (Xam Essentials)


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


        ShinyApplicationValues generatorConfig = new ShinyApplicationValues();
        protected override iOSAppDelegateSourceGenerator Create() => new iOSAppDelegateSourceGenerator
        {
            ShinyConfig = this.generatorConfig
        };


        [Fact(Skip = "iOS Xamarin.Forms lib needs to be included")]
        public void XamarinFormsIntegratedOnFinishedLaunching()
        {
            this.generatorConfig.XamarinFormsAppTypeName = "Tests.MyXfApp";
            this.Generator.AddReference("Xamarin.Forms");
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute(XamarinFormsAppTypeName = ""Tests.MyXfApp"")]

namespace Tests
{
    public partial void MyTestAppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
    }

    public class MyXfApp : global::Xamarin.Forms.Application
    {
    }
}
");
            this.RunGenerator();
            this.CompilationHasContent("this.LoadApplication(new Tests.MyXfApp());", "Xamarin.Forms LoadApplication should have been applied");
            this.CompilationHasContent("global::Xamarin.Forms.Forms.Init();", "Xamarin.Forms.Forms.Init should have been applied");
        }


        [Fact]
        public void ShinyStartupSpecifiedWrong()
        {
            this.generatorConfig.XamarinFormsAppTypeName = "Tests.WrongName";
            this.Generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute(ShinyStartupTypeName = \"Tests.WrongName\")]");
            this.RunGenerator();
            // TODO: should error!
        }


        [Fact]
        public void ShinyStartupSpecified()
        {
            this.generatorConfig.ShinyStartupTypeName = "Tests.MyShinyApp";
            this.Generator.AddSource(StandardAppDelegate);
            this.Generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute(ShinyStartupTypeName = ""Tests.MyShinyApp"")]
namespace Tests
{
    public class MyShinyApp : global::Shiny.ShinyStartup
    {
        public override void ConfigureServices(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services) {}
    }
}");
            this.RunGenerator();
            this.CompilationHasContent("this.ShinyFinishedLaunching(new Tests.MyShinyApp());", "iOS FinishedLaunching should have startup specified");
        }


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


        void CompilationHasContent(string content, string because)
            => this.Compilation.SyntaxTrees.Any(x => x.ToString().Contains(content)).Should().BeTrue(because);
    }
}

using System;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Shiny.Generators.Tests
{
    public class iOSAppDelegateSourceGeneratorTests : IDisposable
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;
        Compilation compile;


        public iOSAppDelegateSourceGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
            this.generator = new AssemblyGenerator();
            this.generator.AddReference("Xamarin.iOS");
            this.generator.AddReference("Shiny");
            this.generator.AddReference("Shiny.Core");

        }


        public void Dispose()
        {
            if (this.compile != null)
                this.output.WriteSyntaxTrees(this.compile);
        }


        [Fact]
        public void PerformFetchGenerated()
        {
            this.generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
namespace MyTest 
{
    public partial class TestAppDelegate : UIKit.UIApplicationDelegate 
    {
    }
}");
            this.compile = this.generator.DoGenerate(
                nameof(PerformFetchGenerated),
                new iOSAppDelegateSourceGenerator()
            );
            var contents = this.compile.SyntaxTrees.Select(x => x.ToString()).ToList();
            contents.FirstOrDefault(x => x.Contains("public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) => this.ShinyPerformFetch(completionHandler);"));
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HandleEventsForBackgroundUrlGeneration(bool includeHttpTransfers)
        {
            if (includeHttpTransfers)
                this.generator.AddReference("Shiny.Net.Http");
        }


        [Theory]
        [InlineData("Shiny.Push")]
        [InlineData("Shiny.Push.AzureNotificationsHubs")]
        [InlineData("Shiny.Push.FirebaseMessaging")]
        [InlineData("Shiny.Push.OneSignal")]
        public void PushEventsGenerated(string libraryToInclude)
        {
            this.generator.AddReference(libraryToInclude);
            this.generator.AddReference("Shiny.Push.Abstractions");

            this.generator.AddSource(@"
[assembly: Shiny.ShinyApplicationAttribute]
namespace MyTest
{
    public partial class TestAppDelegate : UIKit.UIApplicationDelegate
    {
    }
}");
            this.compile = this.generator.DoGenerate(
                nameof(PushEventsGenerated),
                new iOSAppDelegateSourceGenerator()
            );

            var contents = this.compile.SyntaxTrees.Select(x => x.ToString()).ToList();
            contents.FirstOrDefault(x => x.Contains("public partial class TestAppDelegate {")).Should().NotBeNull("Generated app delegate was ");
            contents.FirstOrDefault(x => x.Contains(""));
        }

        // TODO: build xam ios libs, add appdelegate
        // test for finding custom shiny startup
        // test for auto-gen shiny startup
        // test for methods
        // test with/out XF
        // test for auto-init of 3rd party
        // ensure appdelegate methods are generated only when necessary!
    }
}

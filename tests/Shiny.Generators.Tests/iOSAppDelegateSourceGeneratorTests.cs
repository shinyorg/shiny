using System;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis;


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
            this.generator.AddReference("Shiny.Push");
            this.generator.AddReference("Shiny.Push.Abstractions");
        }


        public void Dispose()
        {
            if (this.compile != null)
                this.output.WriteSyntaxTrees(this.compile);
        }


        [Fact]
        public void Test()
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
                nameof(Test),
                new iOSAppDelegateSourceGenerator()
            );
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

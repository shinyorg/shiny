using System;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class iOSAppDelegateSourceGeneratorTests
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;


        public iOSAppDelegateSourceGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
            this.generator = new AssemblyGenerator();
            this.generator.AddReference("Xamarin.iOS");
            this.generator.AddReference("Shiny");
            this.generator.AddReference("Shiny.Core");
        }


        [Fact]
        public void Test()
        {

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

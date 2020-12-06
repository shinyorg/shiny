using System;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class AndroidActivitySourceGeneratorTests : AbstractSourceGeneratorTests<AndroidActivitySourceGenerator>
    {
        // TODO: Xam Essentials
        // TODO: Shiny Hooks
        // TODO: Xam Forms Init/LoadApplication
        public AndroidActivitySourceGeneratorTests(ITestOutputHelper output) : base(output, "Mono.Android", "Shiny", "Shiny.Core") { }


        [Fact]
        public void Test()
        {
        }
    }
}

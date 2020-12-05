using System;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class AndroidActivitySourceGeneratorTests : AbstractSourceGeneratorTests<AndroidActivitySourceGenerator>
    {
        public AndroidActivitySourceGeneratorTests(ITestOutputHelper output) : base(output, "Mono.Android", "Shiny", "Shiny.Core") { }


        [Fact]
        public void Test()
        {
        }
    }
}

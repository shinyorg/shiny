using System;
using Shiny.Generators;
using Xunit.Abstractions;


namespace Shiny.Tests.Generators
{
    public class AndroidActivitySourceGeneratorTests : AbstractSourceGeneratorTests<AndroidActivitySourceGenerator>
    {
        // TODO: test msal?
        // TODO: Xam Essentials
        // TODO: Shiny Hooks
        // TODO: Xam Forms Init/LoadApplication
        public AndroidActivitySourceGeneratorTests(ITestOutputHelper output) : base(output, "Mono.Android", "Shiny", "Shiny.Core") { }
    }
}

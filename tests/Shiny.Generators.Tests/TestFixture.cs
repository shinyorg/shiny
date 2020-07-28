using System;
using Shiny.Testing;
using Xunit;


namespace Shiny.Generators.Tests
{
    public class TestFixture
    {
        [Fact]
        public void DidGenerate()
        {
            TestShinyHost.Init(new AppShinyStartup());
            // TODO: ensure registrations are registered (problem is, none will be since this is netstandard project)
        }
    }
}

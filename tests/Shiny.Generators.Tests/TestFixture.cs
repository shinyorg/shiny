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
            //TestShinyHost.Resolve<>()
            // ensure registrations
        }
    }
}

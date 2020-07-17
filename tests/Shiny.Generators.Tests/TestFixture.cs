using System;
using Shiny.Testing;


namespace Shiny.Generators.Tests
{
    public class TestFixture
    {
        public void Test()
        {
            TestShinyHost.Init(new AppShinyStartup());
            TestShinyHost.Resolve<>()
            // ensure registrations
        }
    }
}

using System;
using Xunit;


namespace Shiny.Tests
{
    [Trait("Category", "Platform")]
    public class PlatformTests
    {
        [Fact]
        public void AppBuild()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().AppBuild);


        [Fact]
        public void AppIdentifier()
            => Assert.Equal("com.shiny.devicetests", ShinyHost.Resolve<IPlatform>().AppIdentifier);


        [Fact]
        public void AppVersion()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().AppVersion);


        // TODO: based on device
        [Fact]
        public void MachineName()
            => Assert.Equal("", ShinyHost.Resolve<IPlatform>().MachineName);

        [Fact]
        public void Manufacturer()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().Manufacturer);

        [Fact]
        public void Model()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().Model);

        [Fact]
        public void OperatingSystem()
            => Assert.Equal("1", ShinyHost.Resolve<IPlatform>().OperatingSystem);
    }
}

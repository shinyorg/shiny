using System;
using Xunit;


namespace Shiny.Device.Tests
{
    public class EnvironmentTests
    {
        [Fact]
        public void AppBuild()
            => Assert.Equal("1", ShinyHost.Resolve<IEnvironment>().AppBuild);


        [Fact]
        public void AppIdentifier()
            => Assert.Equal("com.shiny.devicetests", ShinyHost.Resolve<IEnvironment>().AppIdentifier);


        [Fact]
        public void AppVersion()
            => Assert.Equal("1", ShinyHost.Resolve<IEnvironment>().AppVersion);


        // TODO: based on device
        [Fact]
        public void MachineName()
            => Assert.Equal("", ShinyHost.Resolve<IEnvironment>().MachineName);

        [Fact]
        public void Manufacturer()
            => Assert.Equal("1", ShinyHost.Resolve<IEnvironment>().Manufacturer);

        [Fact]
        public void Model()
            => Assert.Equal("1", ShinyHost.Resolve<IEnvironment>().Model);

        [Fact]
        public void OperatingSystem()
            => Assert.Equal("1", ShinyHost.Resolve<IEnvironment>().OperatingSystem);
    }
}

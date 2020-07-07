using System;
using FluentAssertions;
using Xunit;


namespace Shiny.BluetoothLE.RefitClient.Tests
{
    public class Tests
    {
        [Fact]
        public void GetInstance()
        {
            var impl = BleClientFactory.GetInstance<ITestClient>();
            impl.Should().NotBeNull();
        }
    }
}

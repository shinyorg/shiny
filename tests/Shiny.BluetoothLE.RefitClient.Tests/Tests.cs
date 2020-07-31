using System;
using FluentAssertions;
using Moq;
using Xunit;


namespace Shiny.BluetoothLE.RefitClient.Tests
{
    public class Tests
    {
        [Fact]
        public void GetInstance()
        {
            var mock = new Mock<IPeripheral>();
            var impl = mock.Object.GetClient<ITestClient>();
            impl.Should().NotBeNull();
        }
    }
}

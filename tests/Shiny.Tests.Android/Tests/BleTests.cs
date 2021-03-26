using System;
using FluentAssertions;
using Shiny.BluetoothLE;
using Xunit;


namespace Shiny.Tests.Android.Tests
{
    public class BleTests
    {
        public BleTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseBleClient()
            });
        }


        [Fact]
        public void ControlAdapterStates()
        {
            var manager = (ICanControlAdapterState)ShinyHost.Resolve<IBleManager>();

            var stateChanges = 0;
            manager.WhenStatusChanged().Subscribe(_ => stateChanges++);

            manager.Status.Should().Be(AccessState.Available);
            manager.SetAdapterState(false);

            manager.Status.Should().Be(AccessState.Disabled);
            manager.SetAdapterState(true);

            manager.Status.Should().Be(AccessState.Available);
            stateChanges.Should().Be(3);
        }
    }
}
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Shiny.Infrastructure;
using Xunit;


namespace Shiny.Tests.Core.Infrastructure
{
    public class BusTest
    {
        public string Value { get; set; }
    }


    public class MessageBusTests
    {
        [Fact]
        public async Task EndToEnd()
        {
            var bus = new MessageBus();
            var count = 0;

            bus.Listener<BusTest>().Subscribe(_ => count++);
            bus.Publish(new BusTest { Value = "1" });
            bus.Publish(new BusTest { Value = "2" });
            bus.Publish(new object());
            await Task.Delay(1000);
            count.Should().Be(2);
        }


        [Fact]
        public async Task NamedEndToEnd()
        {
            var bus = new MessageBus();
            var count = 0;

            bus.Listener<BusTest>("test").Subscribe(_ => count++);
            bus.Publish("test", new BusTest { Value = "1" });
            bus.Publish("test", new BusTest { Value = "2" });
            bus.Publish("test", new object());
            bus.Publish(new BusTest());
            await Task.Delay(1000);
            count.Should().Be(2);
        }
    }
}

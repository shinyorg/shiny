using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Shiny.Infrastructure;
using Xunit;


namespace Shiny.Tests
{
    public class BusTest
    {
        public string Value { get; set; }
    }


    public class MessageBusTests
    {
        [Fact]
        public async Task Test()
        {
            var bus = new MessageBus();

            var task = bus.Listener<BusTest>().Take(2).ToList().ToTask();
            bus.Publish(new BusTest {  Value = "1" });
            bus.Publish(new BusTest { Value = "2" });
            bus.Publish(new object());
            var results = await task;

            results.Count.Should().Be(2);
        }
    }
}

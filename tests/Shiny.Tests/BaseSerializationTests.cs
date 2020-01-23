using System;
using FluentAssertions;
using Shiny.Infrastructure;
using Shiny.Locations;
using Xunit;


namespace Shiny.Tests
{
    public abstract class BaseSerializationTests<T> where T : ISerializer
    {
        protected abstract T Create();

        protected TValue SerializeDeserialize<TValue>(TValue start)
        {
            var serializer = this.Create();
            var value = serializer.Serialize(start);
            return serializer.Deserialize<TValue>(value);
        }

        [Fact]
        public virtual void Position()
        {
            var start = new Position(1.23, 4.56);

            var end = this.SerializeDeserialize(start);

            end.Should().Be(start);
        }

        [Fact]
        public virtual void GeofenceRegion()
        {
            var start = new GeofenceRegion("testing", new Position(59.1, 62.5), Distance.FromKilometers(99));

            var end = this.SerializeDeserialize(start);

            end.Should().Be(start); // Identifier
            end.Radius.Should().Be(start.Radius);
            end.Center.Should().Be(start.Center);
        }
    }
}

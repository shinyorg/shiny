using System.Collections.Generic;
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
            var start = new GeofenceRegionStore
            {
                Identifier = "testing",
                CenterLatitude = 59.1,
                CenterLongitude = 62.5,
                RadiusMeters = 99,
                Payload = new Dictionary<string, object>
                {
                    { "String", "Test Geofence" },
                    { "Number", 3.14 },
                    { "Bool", true },
                },
            };

            var end = this.SerializeDeserialize(start);

            end.Should().BeEquivalentTo(start);
            end.RadiusMeters.Should().Be(start.RadiusMeters);
            end.CenterLatitude.Should().Be(start.CenterLatitude);
            end.CenterLongitude.Should().Be(start.CenterLongitude);
            end.Payload.Should().BeEquivalentTo(start.Payload);
        }
    }
}

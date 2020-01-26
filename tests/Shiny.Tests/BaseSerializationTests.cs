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
        protected abstract void DeserializedCollectionShouldBeNull<TValue>(IEnumerable<TValue> collection);


        protected TValue SerializeDeserialize<TValue>(TValue start)
        {
            var serializer = this.Create();
            var value = serializer.Serialize(start);
            return serializer.Deserialize<TValue>(value);
        }

        [Fact]
        public virtual void GeofenceRegion_without_Payload()
        {
            var start = new GeofenceRegion("testing", new Position(59.1, 62.5), Distance.FromMeters(99));

            var end = (GeofenceRegion)this.SerializeDeserialize((GeofenceRegionStore)start);

            end.Should().BeEquivalentTo(start);
            end.Radius.Should().Be(start.Radius);
            end.Center.Should().Be(start.Center);

            DeserializedCollectionShouldBeNull(end.Payload);
        }

        [Fact]
        public virtual void GeofenceRegion_with_Payload()
        {
            var start = new GeofenceRegion("testing", new Position(59.1, 62.5), Distance.FromMeters(99))
            {
                Payload = new Dictionary<string, object>
                {
                    { "String", "Test Geofence" },
                    { "Number", 3.14 },
                    { "Bool", true },
                },
            };

            var end = (GeofenceRegion)this.SerializeDeserialize((GeofenceRegionStore)start);

            end.Should().BeEquivalentTo(start);
            end.Radius.Should().Be(start.Radius);
            end.Center.Should().Be(start.Center);
            end.Payload.Should().BeEquivalentTo(start.Payload);
        }
    }
}

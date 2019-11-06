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


        [Fact(Skip = "TODO")]
        public virtual void GeofenceRegion()
        {
            var start = new GeofenceRegion("testing", new Position(59.1, 62.5), Distance.FromKilometers(99));
            var serializer = this.Create();
            var value = serializer.Serialize(start);
            var end = serializer.Deserialize<GeofenceRegion>(value);
            start.Should().Be(end);
        }
    }
}

using System.Collections.Generic;
using FluentAssertions;
using Shiny.Infrastructure;

namespace Shiny.Tests.Infrastructure
{
    public class JsonNetSerializerTests : BaseSerializationTests<JsonNetSerializer>
    {
        protected override JsonNetSerializer Create()
            => new JsonNetSerializer();

        protected override void DeserializedCollectionShouldBeNull<TValue>(IEnumerable<TValue> collection) =>
            collection.Should().BeNull();
    }
}

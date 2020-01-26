using System.Collections.Generic;
using FluentAssertions;
using Shiny.Infrastructure;

namespace Shiny.Tests.Infrastructure
{
    public class XmlSerializerImplTests : BaseSerializationTests<XmlSerializerImpl>
    {
        protected override XmlSerializerImpl Create()
            => new XmlSerializerImpl();

        // XmlSerializer deserializes List without elements as empty
        protected override void DeserializedCollectionShouldBeNull<TValue>(IEnumerable<TValue> collection) =>
            collection.Should().BeEmpty();
    }
}

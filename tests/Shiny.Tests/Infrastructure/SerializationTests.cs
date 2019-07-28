using System;
using Shiny.Infrastructure;

namespace Shiny.Tests.Infrastructure
{
    public class SerializationTests : BaseSerializationTests<JsonNetSerializer>
    {
        protected override JsonNetSerializer Create()
            => new JsonNetSerializer();
    }
}

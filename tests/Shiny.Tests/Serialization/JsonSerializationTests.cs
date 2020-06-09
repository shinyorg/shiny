using System;
using Shiny.Infrastructure;


namespace Shiny.Tests.Serialization
{
    public class SerializationTests : BaseSerializationTests<ShinySerializer>
    {
        protected override ShinySerializer Create()
            => new ShinySerializer();
    }
}
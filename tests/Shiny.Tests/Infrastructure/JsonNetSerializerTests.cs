using Shiny.Infrastructure;

namespace Shiny.Tests.Infrastructure
{
    public class JsonNetSerializerTests : BaseSerializationTests<JsonNetSerializer>
    {
        protected override JsonNetSerializer Create()
            => new JsonNetSerializer();
    }
}

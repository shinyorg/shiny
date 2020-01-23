using Shiny.Infrastructure;

namespace Shiny.Tests.Infrastructure
{
    public class XmlSerializerImplTests : BaseSerializationTests<XmlSerializerImpl>
    {
        protected override XmlSerializerImpl Create()
            => new XmlSerializerImpl();
    }
}

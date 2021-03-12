using System;
using System.Linq;
using Shiny.Infrastructure;
using Shiny.Testing;


namespace Shiny.Tests.Core.Repositories
{
    public class FileSystemRepositoryTests : BaseRepositoryTests
    {
        readonly TestPlatform platform;
        readonly ShinySerializer serializer;


        public FileSystemRepositoryTests()
        {
            this.serializer = new ShinySerializer();
            this.platform = new TestPlatform();

            this.platform.AppData.GetFiles("*.core").ToList().ForEach(x => x.Delete());
        }


        protected override IRepository Create() => new FileSystemRepositoryImpl(
            this.platform,
            this.serializer
        );
    }
}

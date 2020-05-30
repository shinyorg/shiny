using System;
using System.Linq;
using Shiny.Infrastructure;


namespace Shiny.Tests.Repositories
{
    public class FileSystemRepositoryTests : BaseRepositoryTests
    {
        readonly ShinySerializer serializer;
        readonly FileSystemImpl fileSystem;


        public FileSystemRepositoryTests()
        {
            this.serializer = new ShinySerializer();
            this.fileSystem = new FileSystemImpl();

            this.fileSystem.AppData.GetFiles("*.core").ToList().ForEach(x => x.Delete());
        }


        protected override IRepository Create() => new FileSystemRepositoryImpl(
            this.fileSystem,
            this.serializer
        );
    }
}

using System;
using System.Linq;
using Shiny.Infrastructure;


namespace Shiny.Tests.Infrastructure
{
    public class FileSystemRepositoryTests : BaseRepositoryTests<FileSystemRepositoryImpl>
    {
        readonly ShinySerializer serializer;
        readonly FileSystemImpl fileSystem;


        public FileSystemRepositoryTests()
        {
            this.serializer = new ShinySerializer();
            this.fileSystem = new FileSystemImpl();

            this.fileSystem.AppData.GetFiles("*.core").ToList().ForEach(x => x.Delete());
        }



        protected override FileSystemRepositoryImpl Create() => new FileSystemRepositoryImpl(
            this.fileSystem,
            this.serializer
        );
    }
}

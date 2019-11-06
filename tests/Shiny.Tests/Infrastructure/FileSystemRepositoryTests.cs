using System;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using FluentAssertions;
using Xunit;


namespace Shiny.Tests.Infrastructure
{
    public class FileSystemRepositoryTests : BaseRepositoryTests<FileSystemRepositoryImpl>
    {
        readonly JsonNetSerializer serializer;
        readonly FileSystemImpl fileSystem;


        public FileSystemRepositoryTests()
        {
            this.serializer = new JsonNetSerializer();
            this.fileSystem = new FileSystemImpl();

            this.fileSystem.AppData.GetFiles("*.core").ToList().ForEach(x => x.Delete());
        }



        protected override FileSystemRepositoryImpl Create() => new FileSystemRepositoryImpl(
            this.fileSystem,
            this.serializer
        );
    }
}

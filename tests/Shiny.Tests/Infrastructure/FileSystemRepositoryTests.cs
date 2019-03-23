using System;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using FluentAssertions;
using Xunit;


namespace Shiny.Tests.Infrastructure
{
    public class TestModel
    {
        public string Value1 { get; set; }
        public Type Value2 { get; set; }
    }


    public class TestModel2
    {
        public string Value1 { get; set; }
    }

    public class FileSystemRepositoryTests
    {
        readonly JsonNetSerializer serializer;
        readonly FileSystemImpl fileSystem;


        public FileSystemRepositoryTests()
        {
            this.serializer = new JsonNetSerializer();
            this.fileSystem = new FileSystemImpl();

            this.fileSystem.AppData.GetFiles("*.core").ToList().ForEach(x => x.Delete());
        }


        [Fact]
        public async Task PersistTests()
        {
            var repo1 = this.Create();
            await repo1.Set("value1", new TestModel
            {
                Value1 = "value1",
                Value2 = typeof(FileSystemRepositoryTests)
            });

            var repo2 = this.Create();
            var obj = await repo2.Get<TestModel>("value1");

            obj.Should().NotBeNull();
            obj.Value1.Should().Be("value1");
            obj.Value2.Should().Be<FileSystemRepositoryTests>();
        }


        [Fact]
        public async Task UpdateTest()
        {
            var repo = this.Create();
            await repo.Set(nameof(this.UpdateTest), new TestModel
            {
                Value1 = "1"
            });
            await repo.Set(nameof(this.UpdateTest), new TestModel
            {
                Value1 = "2"
            });
            var results = await repo.GetAll<TestModel>();
            results.Count.Should().Be(1);
            results.First().Value1.Should().Be("2");
        }


        [Fact]
        public async Task MultipleModels()
        {
            var repo = this.Create();
            await repo.Set("1", new TestModel { Value1 = "1" });
            await repo.Set("2", new TestModel { Value1 = "2" });

            var all = await repo.GetAll<TestModel>();
            all.Count.Should().Be(2);

            all.First(x => x.Value1.Equals("1")).Should().NotBeNull();
            all.First(x => x.Value1.Equals("2")).Should().NotBeNull();
        }


        [Fact]
        public async Task TypeSeggregation()
        {
            var repo = this.Create();
            await repo.Set("1", new TestModel {Value1 = "1"});
            await repo.Set("1", new TestModel2 {Value1 = "1"});

            var r1 = await repo.GetAll<TestModel>();
            r1.Count.Should().Be(1);

            var r2 = await repo.GetAll<TestModel2>();
            r2.Count.Should().Be(1);
        }


        [Fact]
        public async Task RemoveTest()
        {
            var repo1 = this.Create();
            await repo1.Set("1", new TestModel {Value1 = "1"});
            await repo1.Remove<TestModel>("1");

            var repo2 = this.Create();
            var result = await repo2.Get<TestModel>("1");
            result.Should().BeNull();
        }


        [Fact]
        public async Task ClearTest()
        {
            await this.MultipleModels();
            var repo = this.Create();
            await repo.Clear<TestModel>();

            var r = await repo.GetAll<TestModel>();
            r.Count.Should().Be(0);
        }


        FileSystemRepositoryImpl Create() => new FileSystemRepositoryImpl(
            this.fileSystem,
            this.serializer
        );
    }
}

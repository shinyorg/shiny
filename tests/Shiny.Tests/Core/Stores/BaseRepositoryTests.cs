using FluentAssertions;
using Shiny.Stores;
using Xunit;

namespace Shiny.Tests.Core.Stores;


public abstract class BaseRepositoryTests
{
    protected abstract IRepository<TestModel> Create();


    [Trait("Category", "Repository")]
    [Fact]
    public async Task PersistTests()
    {
        var repo1 = this.Create();
        var num = new Random().Next(1, 9999);

        await repo1.Set(new TestModel
        {
            Identifier = "value1",
            IntValue = num
        });

        var repo2 = this.Create();
        var obj = await repo2.Get("value1");

        obj.Should().NotBeNull();
        obj.Identifier.Should().Be("value1");
        obj.IntValue.Should().Be(num);
    }


    [Trait("Category", "Repository")]
    [Fact]
    public async Task UpdateTest()
    {
        var repo = this.Create();
        await repo.Set(new TestModel
        {
            Identifier = "1",
            IntValue = 1
        });
        await repo.Set(new TestModel
        {
            Identifier = "1",
            IntValue = 2
        });
        var results = await repo.GetList();
        results.Count.Should().Be(1);
        results.First().IntValue.Should().Be(2);
    }


    [Trait("Category", "Repository")]
    [Fact]
    public async Task MultipleModels()
    {
        var repo = this.Create();
        await repo.Set(new TestModel { Identifier = "1" });
        await repo.Set(new TestModel { Identifier = "2" });

        var all = await repo.GetList();
        all.Count.Should().Be(2);

        all.First(x => x.Identifier.Equals("1")).Should().NotBeNull();
        all.First(x => x.Identifier.Equals("2")).Should().NotBeNull();
    }


    [Trait("Category", "Repository")]
    [Fact]
    public async Task RemoveTest()
    {
        var repo1 = this.Create();
        await repo1.Set(new TestModel { Identifier = "1" });
        await repo1.Remove("1");

        var repo2 = this.Create();
        var result = await repo2.Get("1");
        result.Should().BeNull();
    }


    [Trait("Category", "Repository")]
    [Fact]
    public async Task ClearTest()
    {
        await this.MultipleModels();
        var repo = this.Create();
        await repo.Clear();

        var r = await repo.GetList();
        r.Count.Should().Be(0);
    }
}

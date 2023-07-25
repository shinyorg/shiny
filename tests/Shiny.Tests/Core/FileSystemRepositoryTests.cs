using Shiny.Stores;
using Shiny.Stores.Impl;
using Shiny.Support.Repositories;

namespace Shiny.Tests;


public class FileSystemRepositoryTests : AbstractShinyTests
{
    public FileSystemRepositoryTests(ITestOutputHelper output) : base(output)
    {
    }


    protected override void Configure(HostBuilder hostBuilder)
    {
        base.Configure(hostBuilder);
        hostBuilder.Services.AddSingleton<ISerializer, DefaultSerializer>();
        hostBuilder.Services.AddDefaultRepository();
    }



    public override void Dispose()
    {
        this.GetService<IRepository>().Clear<RepoTest>();
        base.Dispose();
    }


    [Fact(DisplayName = "Repository - Count Watcher")]
    public void CountWatcherTest()
    {
        var repo = this.GetService<IRepository>();
        var count = 0;
        var tester = new RepoTest(Guid.NewGuid().ToString(), "Test1");

        using var sub = repo.CreateCountWatcher<RepoTest>().Subscribe(x => count = x);
        count.Should().Be(0);
        repo.Set(tester);

        count.Should().Be(1);
        repo.Remove(tester);
        count.Should().Be(0);

        repo.Set(tester);
        count.Should().Be(1);
        repo.Clear<RepoTest>();

        count.Should().Be(0);
    }


    [Fact(DisplayName = "Repository - Inserts")]
    public void InsertTest()
    {
        var repo = this.GetService<IRepository>();

        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        repo.Insert(new RepoTest(id1, "Message1"));
        repo.Get<RepoTest>(id1).Should().NotBeNull("1 not found");

        repo.Insert(new RepoTest(id2, "Message2"));
        repo.Get<RepoTest>(id2).Should().NotBeNull("2 not found");
    }
}


public record RepoTest(string Identifier, string Message) : IRepositoryEntity;
using Shiny.Infrastructure.Impl;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Tests.Core.Stores;


// TODO: test beacon regions, channels, notifications, geofence regions, jobs, http transfer stores
public class JsonFileRepositoryTests : BaseRepositoryTests
{

    public JsonFileRepositoryTests()
        => Utils.GetPlatform().AppData.GetFiles("*.shiny").ToList().ForEach(x => x.Delete());


    protected override IRepository<TestModel> Create() => new JsonFileRepository<TestModelStore, TestModel>(
        Utils.GetPlatform(),
        new DefaultSerializer()
    );
}

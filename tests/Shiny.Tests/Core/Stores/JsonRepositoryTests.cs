using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Tests.Core.Stores;


public class JsonFileRepositoryTests : BaseRepositoryTests
{

    public JsonFileRepositoryTests()
        => Utils.GetPlatform().AppData.GetFiles("*.shiny").ToList().ForEach(x => x.Delete());


    protected override IRepository<TModel> Create<TModel, TConverter>()
    {
        return new JsonFileRepository<TConverter, TModel>(
            Utils.GetPlatform(),
            new DefaultSerializer()
        );
    }
}

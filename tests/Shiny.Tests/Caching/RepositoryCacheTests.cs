using System;
using Shiny.Caching;
using Shiny.Infrastructure;
using Shiny.Testing.Infrastucture;


namespace Shiny.Tests.Caching
{
    public class RepositoryCacheTests : BaseCacheTests
    {
        protected override ICache Create() => new RepositoryCache(
            new InMemoryRepository(), 
            new ShinySerializer()
        );
    }
}

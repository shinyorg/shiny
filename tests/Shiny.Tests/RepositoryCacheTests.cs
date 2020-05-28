using System;
using Shiny.Caching;
using Shiny.Testing.Infrastucture;


namespace Shiny.Tests
{
    public class RepositoryCacheTests : BaseCacheTests<RepositoryCache>
    {
        protected override RepositoryCache Create() => new RepositoryCache(new InMemoryRepository());
    }
}

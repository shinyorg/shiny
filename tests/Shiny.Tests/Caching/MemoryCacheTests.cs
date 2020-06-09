using System;
using Shiny.Caching;


namespace Shiny.Tests.Caching
{
    public class MemoryCacheTests : BaseCacheTests
    {
        protected override ICache Create() => new MemoryCache();
    }
}

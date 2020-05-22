using System;
using Shiny.Caching;


namespace Shiny.Tests
{
    public class MemoryCacheTests : BaseCacheTests<MemoryCache>
    {
        protected override MemoryCache Create() => new MemoryCache();
    }
}

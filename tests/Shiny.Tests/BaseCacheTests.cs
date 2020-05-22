using System;
using System.Threading.Tasks;
using FluentAssertions;
using Shiny.Caching;
using Xunit;


namespace Shiny.Tests
{
    public abstract class BaseCacheTests<T> where T : ICache
    {
        protected abstract T Create();


        [Fact]
        public async Task Timer()
        {
            var cache = this.Create();
            await cache.Set("Test", "test me");
            await Task.Delay(1000);
            var result = await cache.Get<string>("Test");
            result.Should().BeNull();
        }



        [Fact]
        public async Task ShouldReturnNull()
        {
            var cache = this.Create();
            //cache.TryGet("", async () => { });
        }
    }
}

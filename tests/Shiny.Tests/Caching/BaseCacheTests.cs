using System;
using System.Threading.Tasks;
using FluentAssertions;
using Shiny.Caching;
using Xunit;


namespace Shiny.Tests.Caching
{

    public class CacheTestObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Value { get; set; }
    }


    public abstract class BaseCacheTests
    {
        protected abstract ICache Create();


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
        public async Task TryGetTest()
        {
            var cache = this.Create();

            var value1 = await cache.TryGet(
                nameof(TryGetTest), 
                () => Task.FromResult(new CacheTestObject { Value = nameof(TryGetTest) })
            );
            var value2 = await cache.Get<CacheTestObject>(nameof(TryGetTest));

            value1.Id.Should().Be(value2.Id);
            value1.Value.Should().Be(value2.Value);
        }
    }
}

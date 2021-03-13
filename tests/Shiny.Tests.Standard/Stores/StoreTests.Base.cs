using System;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Stores
{
    public partial class StoreTests
    {
        [Trait("Category", "Base")]
        [Theory]
        [MemberData(nameof(Data))]
        public void Set(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Set("Test", "1");
            this.currentStore.Set("Test", "2");
            var r = this.currentStore.Get<string>("Test");
            Assert.Equal("2", r);
        }


        [Trait("Category", "Base")]
        [Theory]
        [MemberData(nameof(Data))]
        public void ContainsTest(IKeyValueStore store)
        {
            this.currentStore = store;
            Assert.False(this.currentStore.Contains(Guid.NewGuid().ToString()), "Contains should have returned false");

            this.currentStore.Set("Test", "1");
            Assert.True(this.currentStore.Contains("Test"), "Contains should have returned true");
        }


        [Trait("Category", "Base")]
        [Theory]
        [MemberData(nameof(Data))]
        public void RemoveTest(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Set("Test", "1");
            Assert.True(this.currentStore.Remove("Test"), "Remove should have returned success");
        }
    }
}

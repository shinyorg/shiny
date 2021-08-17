using System;
using FluentAssertions;
using Xunit;
using Shiny.Stores;


namespace Shiny.Tests.Core.Stores
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
            this.currentStore.Get(typeof(string), "Test").Should().Be("2");
        }


        [Trait("Category", "Base")]
        [Theory]
        [MemberData(nameof(Data))]
        public void ContainsTest(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Contains(Guid.NewGuid().ToString()).Should().BeFalse("Contains should have returned false");

            this.currentStore.Set("Test", "1");
            this.currentStore.Contains("Test").Should().BeTrue("Contains should have returned true");
        }


        [Trait("Category", "Base")]
        [Theory]
        [MemberData(nameof(Data))]
        public void RemoveTest(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Set("Test", "1");
            this.currentStore.Remove("Test").Should().BeTrue("Remove should have returned success");
        }
    }
}

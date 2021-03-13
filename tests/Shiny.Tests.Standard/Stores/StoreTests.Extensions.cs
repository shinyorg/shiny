using System;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Stores
{
    public partial class StoreTests
    {
        [Trait("Category", "Extensions")]
        [Theory]
        [MemberData(nameof(Data))]
        public void SetDefault(IKeyValueStore store)
        {
            this.currentStore = store;
            Assert.True(this.currentStore.SetDefault(nameof(SetDefault), "Initial Value"), "Default value could not be set");
            Assert.False(this.currentStore.SetDefault(nameof(SetDefault), "Second Value"), "Default value was set and should not have been");
            Assert.Equal("Initial Value", this.currentStore.Get<string>(nameof(SetDefault)));
        }
    }
}

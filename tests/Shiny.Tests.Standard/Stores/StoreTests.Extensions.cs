using System;
using FluentAssertions;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Stores
{
    public partial class StoreTests
    {
        [Trait("Category", "Extensions")]
        [Theory]
        [MemberData(nameof(Data))]
        public void SetT(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Get<int>(nameof(SetT)).Should().Be(0);

            this.currentStore.Set(nameof(SetT), 345);
            this.currentStore.Get<int>(nameof(SetT)).Should().Be(345);
        }


        [Trait("Category", "Extensions")]
        [Theory]
        [MemberData(nameof(Data))]
        public void SetT_Nullable(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Get<Guid>(nameof(SetT_Nullable)).Should().Be(Guid.Empty);
            this.currentStore.Get<Guid?>(nameof(SetT_Nullable)).Should().BeNull();

            var guid = Guid.NewGuid();
            this.currentStore.Set(nameof(SetT_Nullable), guid);
            this.currentStore.Get<Guid?>(nameof(SetT_Nullable)).Should().Be(guid);
        }


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

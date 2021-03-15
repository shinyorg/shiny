using System;
using FluentAssertions;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Core.Stores
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
        public void GuidTests(IKeyValueStore store)
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
        public void Bools(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Get<bool>(nameof(Bools)).Should().BeFalse("Nothing set - should be return false");
            this.currentStore.Get<bool?>(nameof(Bools)).Should().BeNull("Nothing set - nullable allowed - but not returning null");

            this.currentStore.Set(nameof(Bools), true);
            this.currentStore.Get<bool>(nameof(Bools)).Should().BeTrue("Value should now be true");
        }


        [Trait("Category", "Extensions")]
        [Theory]
        [MemberData(nameof(Data))]
        public void SetDefault(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.SetDefault(nameof(SetDefault), "Initial Value").Should().BeTrue("Default value could not be set");
            this.currentStore.SetDefault(nameof(SetDefault), "Second Value").Should().BeFalse("Default value was set and should not have been");
            this.currentStore.Get<string>(nameof(SetDefault)).Should().Be("Initial Value");
        }
    }
}

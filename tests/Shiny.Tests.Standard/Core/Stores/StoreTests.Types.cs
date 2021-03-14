using System;
using FluentAssertions;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Core.Stores
{
    public partial class StoreTests
    {
        [Trait("Category", "Types")]
        [Theory]
        [MemberData(nameof(Data))]
        public void DateTimeOffsetTest(IKeyValueStore store)
        {
            this.currentStore = store;
            var dt = DateTimeOffset.Now;
            this.currentStore.Set("now", dt);
            this.currentStore
                .Get(typeof(DateTimeOffset), "now")
                .Should()
                .Be(dt);
        }


        [Trait("Category", "Types")]
        [Theory]
        [MemberData(nameof(Data))]
        public void EnumTest(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Set(nameof(EnumTest), MyTestEnum.Hi);
            this.currentStore
                .Get(typeof(MyTestEnum), nameof(EnumTest))
                .Should()
                .Be(MyTestEnum.Hi);
        }


        [Trait("Category", "Types")]
        [Theory]
        [MemberData(nameof(Data))]
        public void LongTest(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Set("LongTest", 99L);
            this.currentStore.Get(typeof(long), "LongTest").Should().Be(99L);
        }


        [Trait("Category", "Types")]
        [Theory]
        [MemberData(nameof(Data))]
        public void GuidTest(IKeyValueStore store)
        {
            this.currentStore = store;

            var guid = new Guid();
            this.currentStore.Set(nameof(GuidTest), guid);
            this.currentStore
                .Get(typeof(Guid), nameof(GuidTest))
                .Should()
                .Be(guid);
        }

        //[Theory]
        //[MemberData(nameof(Data))]
        //public virtual void NullableEnums(IKeyValueStore store)
        //{
        //    this.currentStore = store;
        //    var value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
        //    Assert.Null(value);

        //    value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
        //    Assert.Equal(MyTestEnum.Bye, value);

        //    this.currentStore.Set(nameof(NullableEnums), MyTestEnum.Hi);
        //    value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
        //    Assert.Equal(MyTestEnum.Hi, value);

        //    this.currentStore.Set(nameof(NullableEnums), null);
        //    value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
        //    Assert.Null(value);
        //}


        //[Theory]
        //[MemberData(nameof(Data))]
        //public void DateTimeNullTest(IKeyValueStore store)
        //{
        //    this.currentStore = store;
        //    var dt = new DateTime(1999, 12, 31, 23, 59, 0);
        //    var nvalue = this.currentStore.Get<DateTime?>("DateTimeNullTest");
        //    Assert.True(nvalue == null, "Should be null");

        //    this.currentStore.Set("DateTimeNullTest", dt);
        //    nvalue = this.currentStore.Get<DateTime?>("DateTimeNullTest");
        //    Assert.Equal(nvalue, dt);
        //}


        //[Fact]
        //public void NullBools()
        //{
        //    this.currentStore.Set<bool?>("SetNullBool", null);
        //    var value = this.currentStore.Get<bool?>("SetNullBool");
        //    Assert.Null(value);

        //    this.currentStore.Set<bool?>("SetNullBool", true);
        //    value = this.currentStore.Get<bool?>("SetNullBool");
        //    Assert.Equal(true, value);

        //    this.currentStore.Set<bool?>("SetNullBool", false);
        //    value = this.currentStore.Get<bool?>("SetNullBool");
        //    Assert.Equal(false, value);

        //    this.currentStore.Set<bool?>("SetNullBool", null);
        //    value = this.currentStore.Get<bool?>("SetNullBool");
        //    Assert.Null(value);
        //}
    }
}

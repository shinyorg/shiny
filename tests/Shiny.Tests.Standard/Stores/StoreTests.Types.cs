using System;
using FluentAssertions;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Stores
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
            var result = this.currentStore.Get<DateTimeOffset>("now");
            dt.Should().Be(result);
        }


        [Trait("Category", "Types")]
        [Theory]
        [MemberData(nameof(Data))]
        public void EnumTest(IKeyValueStore store)
        {
            this.currentStore = store;
            this.currentStore.Set(nameof(EnumTest), MyTestEnum.Hi);
            var value = this.currentStore.Get<MyTestEnum>(nameof(EnumTest));
            value.Should().Be(MyTestEnum.Hi);
        }


        [Trait("Category", "Types")]
        [Theory]
        [MemberData(nameof(Data))]
        public void LongTest(IKeyValueStore store)
        {
            this.currentStore = store;
            long value = 99;
            this.currentStore.Set("LongTest", value);
            var value2 = this.currentStore.Get<long>("LongTest");
            Assert.Equal(value, value2);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void EmptyGuidTest(IKeyValueStore store)
        {
            this.currentStore = store;
            var empty = this.currentStore.Get<Guid>(nameof(EmptyGuidTest));
            empty.Should().Be(Guid.Empty);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void GuidTest(IKeyValueStore store)
        {
            this.currentStore = store;

            var guid = new Guid();
            this.currentStore.Set(nameof(GuidTest), guid);
            this.currentStore.Get<Guid>(nameof(GuidTest)).Should().Be(guid);
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

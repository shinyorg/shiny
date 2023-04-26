using Shiny.Stores;

namespace Shiny.Tests.Core.Stores;


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
        this.currentStore.Set(nameof(this.EnumTest), MyTestEnum.Hi);
        this.currentStore
            .Get(typeof(MyTestEnum), nameof(this.EnumTest))
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
        this.currentStore.Set(nameof(this.GuidTest), guid);
        this.currentStore
            .Get(typeof(Guid), nameof(this.GuidTest))
            .Should()
            .Be(guid);
    }
}

using Shiny.Stores;

namespace Shiny.Tests.Core.Stores;


public partial class StoreTests
{
    [Theory(DisplayName = "Stores - DateTimeOffset")]
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

    
    [Theory(DisplayName = "Stores - Enums")]
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

    
    [Theory(DisplayName = "Stores - Long/Int32")]
    [MemberData(nameof(Data))]
    public void LongTest(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Set("LongTest", 99L);
        this.currentStore.Get(typeof(long), "LongTest").Should().Be(99L);
    }


    [Theory(DisplayName = "Stores - Guids")]
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


    [Theory(DisplayName = "Stores - Simple Arrays")]
    [MemberData(nameof(Data))]
    public void SimpleArrayTest(IKeyValueStore store)
        => this.DoArrayTest(store, nameof(this.SimpleArrayTest), new [] { 1, 6, 9 });
    
    
    [Theory(DisplayName = "Stores - Object Arrays")]
    [MemberData(nameof(Data))]
    public void ObjectArrayTest(IKeyValueStore store)
        => this.DoArrayTest(store, nameof(this.ObjectArrayTest), new []
        {
            new TestBind
            {
                IntValue = 10,
                StringProperty = "10"
            },
            new TestBind
            {
                IntValue = 22,
                StringProperty = "22"
            } 
        });


    [Theory(DisplayName = "Stores - Enum Arrays")]
    [MemberData(nameof(Data))]
    public void EnumArrayTest(IKeyValueStore store)
        => this.DoArrayTest(store, nameof(this.EnumArrayTest), new[] { MyTestEnum.Hi, MyTestEnum.DontKnow });


    void DoArrayTest<T>(IKeyValueStore store, string storeKey, T[] values)
    {
        this.currentStore = store;
        this.currentStore.Set(storeKey, values);
        var storeValues = this.currentStore.Get<T[]>(storeKey);
        
        storeValues.Should().BeSameAs(values);
    }
}

using System;
using System.ComponentModel;
using System.Linq;
using FluentAssertions;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Core.Stores
{
    public partial class StoreTests
    {

        [Trait("Category", "ObjectBinder")]
        [Theory]
        [MemberData(nameof(Data))]
        public void Binding_Basic(IKeyValueStore store)
        {
            var values = SetupBinder<TestBind>(store);
            values.BoundObject.StringProperty = "Hi";

            store.Contains("TestBind.StringProperty").Should().BeTrue();
            store.Get<string>("TestBind.StringProperty").Should().Be("Hi");
        }


        [Trait("Category", "ObjectBinder")]
        [Theory]
        [MemberData(nameof(Data))]
        public void Binding_Persist(IKeyValueStore store)
        {
            var values = this.SetupBinder<TestBind>(store);
            values.BoundObject.StringProperty = "Binding_Persist";

            var obj2 = new TestBind();
            values.Binder.Bind(obj2, store);
            values.BoundObject.StringProperty.Should().Be(obj2.StringProperty);
        }


        [Trait("Category", "ObjectBinder")]
        [Theory]
        [MemberData(nameof(Data))]
        public void NullifyingRemoves(IKeyValueStore store)
        {
            var values = this.SetupBinder<TestBind>(store);

            values.BoundObject.StringProperty = "Binding_Persist";
            store.Contains("TestBind.StringProperty").Should().BeTrue();

            values.BoundObject.StringProperty = null;
            store.Contains("TestBind.StringProperty").Should().BeFalse();
        }


        [Trait("Category", "ObjectBinder")]
        [Theory]
        [MemberData(nameof(Data))]
        public void DefaultValueRemoves(IKeyValueStore store)
        {
            var values = this.SetupBinder<TestBind>(store);

            values.BoundObject.IntValue = 99;
            store.Get<int>("TestBind.IntValue").Should().Be(99);

            values.BoundObject.IntValue = 0;
            store.Contains("TestBind.IntValue").Should().BeFalse();
        }


        [Trait("Category", "ObjectBinder")]
        [Fact]
        public void AttributeBinding()
        {
            var allStores = Data.Select(x => x.First()).Cast<IKeyValueStore>().ToList();
            var factory = new KeyValueStoreFactory(allStores);
            var binder = new ObjectStoreBinder(factory);

            var obj = new AttributeTestBind();
            var random = Guid.NewGuid().ToString();
            binder.Bind(obj);
            obj.TestString = random;

            factory
                .GetStore("file")
                .Get<string>("AttributeTestBind.TestString")
                .Should()
                .Be(random);
        }


        (IObjectStoreBinder Binder, T BoundObject) SetupBinder<T>(IKeyValueStore store) where T : class, INotifyPropertyChanged, new()
        {
            this.currentStore = store;
            var factory = new KeyValueStoreFactory(new [] { store });
            var binder = new ObjectStoreBinder(factory);

            var obj = new T();
            binder.Bind(obj, store);
            return (binder, obj);
        }
    }
}

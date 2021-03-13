using System;
using System.ComponentModel;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Stores
{
    public class ObjectBinderTests
    {

        //[Theory]
        //[MemberData(nameof(Data))]
        //public void Binding_Basic(string alias)
        //{
        //    var values = GetBinder<TestBind>(alias);
        //    values.BoundObject.StringProperty = "Hi";

        //    Assert.True(this.currentStore.Contains("TestBind.StringProperty"));
        //    Assert.False(this.currentStore.Contains("TestBind.IgnoredProperty"));
        //    Assert.Equal("Hi", this.currentStore.Get<string>("TestBind.StringProperty"));
        //}


        //[Theory]
        //[MemberData(nameof(Data))]
        //public void Binding_Persist(string alias)
        //{
        //    var values = GetBinder<TestBind>(alias);
        //    values.BoundObject.StringProperty = "Binding_Persist";

        //    var obj2 = new TestBind();
        //    values.Binder.Bind(obj2, alias);
        //    Assert.Equal(obj2.StringProperty, values.BoundObject.StringProperty);
        //}



        //(IObjectStoreBinder Binder, T BoundObject) GetBinder<T>(string alias) where T : class, INotifyPropertyChanged, new()
        //{
        //    var binder = ShinyHost.Resolve<IObjectStoreBinder>();
        //    var obj = new T();
        //    binder.Bind(obj, alias);

        //    this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);

        //    return (binder, obj);
        //}
    }
}

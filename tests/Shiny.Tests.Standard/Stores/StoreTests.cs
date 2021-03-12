using System;
using System.Collections.Generic;
using System.ComponentModel;
using Shiny.Stores;
using Xunit;


namespace Shiny.Tests.Stores
{
    public class StoreTests : IDisposable
    {
        IKeyValueStore? currentStore;


        public static IEnumerable<object[]> Data
        {
            get
            {
#if __ANDROID__ || __IOS__
                yield return new object[] { "secure" };
#endif
#if __ANDROID__ || __IOS__ || WINDOWS_UWP
                yield return new object[] { "settings" };
#endif
                yield return new object[] { "file" };
                yield return new object[] { "memory" };
                yield return new object[] { "sqlite" };
            }
        }

        public void Dispose()
        {
            this.currentStore?.Clear();
        }


        // TODO: test flags?
        [Theory]
        [MemberData(nameof(Data))]
        public virtual void NullableEnums(string storeAliasName)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(storeAliasName);
            var value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Null(value);

            value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Equal(MyTestEnum.Bye, value);

            this.currentStore.Set(nameof(NullableEnums), MyTestEnum.Hi);
            value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Equal(MyTestEnum.Hi, value);

            this.currentStore.Set(nameof(NullableEnums), null);
            value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Null(value);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void SetNullRemoves(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            this.currentStore.Set("SetNullRemoves", "Blah");
            this.currentStore.Set("SetNullRemoves", null);
            Assert.False(this.currentStore.Contains("SetNullRemoves"));
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void SetDefault_DoesNotRemove_SimpleType(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            this.currentStore.Set("Bool", false);
            Assert.True(this.currentStore.Contains("Bool"), "Bool is missing");

            this.currentStore.Set("Long", 0L);
            Assert.True(this.currentStore.Contains("Long"), "Long is missing");
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void SetDefault_DoesRemove_ComplexType(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            this.currentStore.Set("Object", new object());
            Assert.True(this.currentStore.Contains("Object"), "Object is missing");

            this.currentStore.Set("Object", null);
            Assert.False(this.currentStore.Contains("Object"), "Object should be missing");
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void Tuples(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            var inv = new Tuple<int, string>(1, "2");
            this.currentStore.Set("Object", inv);

            var outv = this.currentStore.Get<Tuple<int, string>>("Object");
            Assert.Equal(inv.Item1, outv.Item1);
            Assert.Equal(inv.Item2, outv.Item2);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void IntTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            this.currentStore.Set("Test", 99);
            var value = this.currentStore.Get<int>("Test");
            Assert.True(value.Equals(99));
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void IntNullTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            var nvalue = this.currentStore.Get<int?>("Blah");
            Assert.True(nvalue == null, "Int? should be null");

            nvalue = 199;
            this.currentStore.Set("Blah", nvalue);

            nvalue = this.currentStore.Get<int?>("Blah");
            Assert.True(nvalue.Value.Equals(199));
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void DateTimeNullTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            var dt = new DateTime(1999, 12, 31, 23, 59, 0);
            var nvalue = this.currentStore.Get<DateTime?>("DateTimeNullTest");
            Assert.True(nvalue == null, "Should be null");

            this.currentStore.Set("DateTimeNullTest", dt);
            nvalue = this.currentStore.Get<DateTime?>("DateTimeNullTest");
            Assert.Equal(nvalue, dt);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void DateTimeOffsetTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            var dt = DateTimeOffset.Now.TrimDate();
            this.currentStore.Set("now", dt);
            var result = this.currentStore.Get<DateTimeOffset>("now");
            Assert.Equal(dt, result);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void SetOverride(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            this.currentStore.Set("Test", "1");
            this.currentStore.Set("Test", "2");
            var r = this.currentStore.Get<string>("Test");
            Assert.Equal("2", r);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void ContainsTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            Assert.False(this.currentStore.Contains(Guid.NewGuid().ToString()), "Contains should have returned false");

            this.currentStore.Set("Test", "1");
            Assert.True(this.currentStore.Contains("Test"), "Contains should have returned true");
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void RemoveTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            this.currentStore.Set("Test", "1");
            Assert.True(this.currentStore.Remove("Test"), "Remove should have returned success");
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void LongTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            long value = 1;
            this.currentStore.Set("LongTest", value);
            var value2 = this.currentStore.Get<long>("LongTest");
            Assert.Equal(value, value2);
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void GuidTest(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            Assert.Equal(this.currentStore.Get<Guid>("GuidTest"), Guid.Empty);

            var guid = new Guid();
            this.currentStore.Set("GuidTest", guid);
            Assert.Equal(this.currentStore.Get<Guid>("GuidTest"), guid);
        }


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


        //[Fact]
        //public void GetDefaultParameter()
        //{
        //    var tmp = Guid.NewGuid().ToString();
        //    Assert.Equal(this.currentStore.Get("GetDefaultParameter", tmp), tmp);
        //}


        [Theory]
        [MemberData(nameof(Data))]
        public void TryDefaults(string alias)
        {
            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);
            Assert.True(this.currentStore.SetDefault("TryDefaults", "Initial Value"), "Default value could not be set");
            Assert.False(this.currentStore.SetDefault("TryDefaults", "Second Value"), "Default value was set and should not have been");
            Assert.Equal("Initial Value", this.currentStore.Get<string>("TryDefaults"));
        }


        //#if !WINDOWS_UWP

        //        [Fact]
        //        public void CultureFormattingTest()
        //        {
        //            var value = 11111.1111m;
        //            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
        //            this.Settings.Set("CultureFormattingTest", value);
        //            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ja-JP");
        //            var newValue = this.Settings.Get<decimal>("CultureFormattingTest");
        //            newValue.Should().Be(value);
        //        }
        //#endif


        [Theory]
        [MemberData(nameof(Data))]
        public void Binding_Basic(string alias)
        {
            var values = GetBinder<TestBind>(alias);
            values.BoundObject.StringProperty = "Hi";

            Assert.True(this.currentStore.Contains("TestBind.StringProperty"));
            Assert.False(this.currentStore.Contains("TestBind.IgnoredProperty"));
            Assert.Equal("Hi", this.currentStore.Get<string>("TestBind.StringProperty"));
        }


        [Theory]
        [MemberData(nameof(Data))]
        public void Binding_Persist(string alias)
        {
            var values = GetBinder<TestBind>(alias);
            values.BoundObject.StringProperty = "Binding_Persist";

            var obj2 = new TestBind();
            values.Binder.Bind(obj2, alias);
            Assert.Equal(obj2.StringProperty, values.BoundObject.StringProperty);
        }


        (IObjectStoreBinder Binder, T BoundObject) GetBinder<T>(string alias) where T : class, INotifyPropertyChanged, new()
        {
            var binder = ShinyHost.Resolve<IObjectStoreBinder>();
            var obj = new T();
            binder.Bind(obj, alias);

            this.currentStore = ShinyHost.Resolve<IKeyValueStoreFactory>().GetStore(alias);

            return (binder, obj);
        }
    }


    public static class Extensions
    {
        public static DateTimeOffset TrimDate(this DateTimeOffset dt)
            => new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
    }
}
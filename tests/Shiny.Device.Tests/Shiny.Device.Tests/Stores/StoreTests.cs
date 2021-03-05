using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Shiny.Integrations.Sqlite;
using Shiny.Stores;
using Xunit;


namespace Shiny.Device.Tests.Stores
{
    public class StoreTests : IDisposable
    {
        IKeyValueStore currentStore;


        public static IEnumerable<object[]> Data => new List<object[]>
        {
                new object[] { "settings" },
                new object[] { "secure" },
                new object[] { "file" },
                new object[] { "memory" },
                new object[] { "sqlite" }
        };

        public void Dispose()
        {
            this.currentStore.Clear();
        }


        // TODO: test flags?
        [Theory]
        [MemberData(nameof(Data))]
        public virtual void NullableEnums(string storeAliasName)
        {
            var value = this.currentStore.Get<MyTestEnum>(nameof(NullableEnums));
            Assert.Null(value);

            value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Equal(MyTestEnum.Bye, value);

            this.currentStore.Set<MyTestEnum?>(nameof(NullableEnums), MyTestEnum.Hi);
            value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Equal(MyTestEnum.Hi, value);

            this.currentStore.Set<MyTestEnum?>(nameof(NullableEnums), null);
            value = this.currentStore.Get<MyTestEnum?>(nameof(NullableEnums));
            Assert.Null(value);
        }


        [Fact]
        public void SetNullRemoves()
        {
            this.currentStore.Set("SetNullRemoves", "Blah");
            this.currentStore.Set<string>("SetNullRemoves", null);
            Assert.False(this.currentStore.Contains("SetNullRemoves"));
        }


        [Fact]
        public async Task OnSettingChanged()
        {
            var task = this.currentStore.Changed.ToTask();
            this.currentStore.Set("OnSettingChanged", "boo");
            var eventArgs = await task;

            Assert.Equal(SettingChangeAction.Add, eventArgs.Action);
            Assert.Equal("OnSettingChanged", eventArgs.Key);
            Assert.Equal("boo", eventArgs.Value);
        }


        [Fact]
        public void SetDefault_DoesNotRemove_SimpleType()
        {
            this.currentStore.Set("Bool", false);
            Assert.True(this.currentStore.Contains("Bool"), "Bool is missing");

            this.currentStore.Set("Long", 0L);
            Assert.True(this.currentStore.Contains("Long"), "Long is missing");
        }


        [Fact]
        public void SetDefault_DoesRemove_ComplexType()
        {
            this.currentStore.Set("Object", new object());
            Assert.True(this.currentStore.Contains("Object"), "Object is missing");

            this.currentStore.Set<object>("Object", null);
            Assert.False(this.currentStore.Contains("Object"), "Object should be missing");
        }


        [Fact]
        public void Object()
        {
            var inv = new Tuple<int, string>(1, "2");
            this.currentStore.Set("Object", inv);

            var outv = this.currentStore.Get<Tuple<int, string>>("Object");
            Assert.Equal(inv.Item1, outv.Item1);
            Assert.Equal(inv.Item2, outv.Item2);
        }


        [Fact]
        public void IntTest()
        {
            this.currentStore.Set("Test", 99);
            var value = this.currentStore.Get<int>("Test");
            Assert.True(value.Equals(99));
        }


        [Fact]
        public void IntNullTest()
        {
            var nvalue = this.currentStore.Get<int?>("Blah");
            Assert.True(nvalue == null, "Int? should be null");

            nvalue = 199;
            this.currentStore.Set("Blah", nvalue);

            nvalue = this.currentStore.Get<int?>("Blah");
            Assert.True(nvalue.Value.Equals(199));
        }


        [Fact]
        public void DateTimeNullTest()
        {
            var dt = new DateTime(1999, 12, 31, 23, 59, 0);
            var nvalue = this.currentStore.Get<DateTime?>("DateTimeNullTest");
            Assert.True(nvalue == null, "Should be null");

            this.currentStore.Set("DateTimeNullTest", dt);
            nvalue = this.currentStore.Get<DateTime?>("DateTimeNullTest");
            Assert.Equal(nvalue, dt);
        }


        [Fact]
        public void DateTimeOffsetTest()
        {
            var dt = DateTimeOffset.Now.TrimDate();
            this.currentStore.Set("now", dt);
            var result = this.currentStore.Get<DateTimeOffset>("now");
            Assert.Equal(dt, result);
        }


        [Fact]
        public void SetOverride()
        {
            this.currentStore.Set("Test", "1");
            this.currentStore.Set("Test", "2");
            var r = this.currentStore.Get<string>("Test");
            Assert.Equal("2", r);
        }


        [Fact]
        public void ContainsTest()
        {
            Assert.False(this.currentStore.Contains(Guid.NewGuid().ToString()), "Contains should have returned false");

            this.currentStore.Set("Test", "1");
            Assert.True(this.currentStore.Contains("Test"), "Contains should have returned true");
        }


        [Fact]
        public void RemoveTest()
        {
            this.currentStore.Set("Test", "1");
            Assert.True(this.currentStore.Remove("Test"), "Remove should have returned success");
        }


        [Fact]
        public void LongTest()
        {
            long value = 1;
            this.currentStore.Set("LongTest", value);
            var value2 = this.currentStore.Get<long>("LongTest");
            Assert.Equal(value, value2);
        }


        [Fact]
        public void GuidTest()
        {
            Assert.Equal(this.currentStore.Get<Guid>("GuidTest"), Guid.Empty);

            var guid = new Guid();
            this.currentStore.Set("GuidTest", guid);
            Assert.Equal(this.currentStore.Get<Guid>("GuidTest"), guid);
        }


        [Fact]
        public void NullBools()
        {
            this.currentStore.Set<bool?>("SetNullBool", null);
            var value = this.currentStore.Get<bool?>("SetNullBool");
            Assert.Null(value);

            this.currentStore.Set<bool?>("SetNullBool", true);
            value = this.currentStore.Get<bool?>("SetNullBool");
            Assert.Equal(true, value);

            this.currentStore.Set<bool?>("SetNullBool", false);
            value = this.currentStore.Get<bool?>("SetNullBool");
            Assert.Equal(false, value);

            this.currentStore.Set<bool?>("SetNullBool", null);
            value = this.currentStore.Get<bool?>("SetNullBool");
            Assert.Null(value);
        }


        [Fact]
        public void GetDefaultParameter()
        {
            var tmp = Guid.NewGuid().ToString();
            Assert.Equal(this.currentStore.Get("GetDefaultParameter", tmp), tmp);
        }


        [Fact]
        public void TryDefaults()
        {
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


        [Fact]
        [MemberData(nameof(Data))]
        public void Binding_Basic(string alias)
        {
            var obj = new TestBind();
            ShinyHost.Resolve<IObjectStoreBinder>().Bind(obj, alias);
            obj.StringProperty = "Hi";

            Assert.True(this.currentStore.Contains("TestBind.StringProperty"));
            Assert.False(this.currentStore.Contains("TestBind.IgnoredProperty"));
            Assert.Equal("Hi", this.currentStore.Get<string>("TestBind.StringProperty"));
        }


        [Fact]
        public void Binding_Persist()
        {
            var obj = this.currentStore.Bind<TestBind>();
            obj.StringProperty = "Binding_Persist";

            var obj2 = this.currentStore.Bind<TestBind>();
            Assert.Equal(obj2.StringProperty, obj.StringProperty);
        }
    }


    public static class Extensions
    {
        public static DateTimeOffset TrimDate(this DateTimeOffset dt)
            => new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
    }
}
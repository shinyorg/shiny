using System;
using System.Threading.Tasks;
using Shiny.Settings;
using Xunit;


namespace Shiny.Device.Tests.Settings
{
    public abstract class AbstractSettingTests : IDisposable
    {
        public ISettings Settings { get; protected set; }


        public void Dispose()
        {
            this.Settings.Clear();
        }


        [Fact]
        public async Task OnSettingChanged()
        {
            var tcs = new TaskCompletionSource<SettingChangeEventArgs>();
            this.Settings.Changed += (sender, args) => tcs.TrySetResult(args);

            this.Settings.Set("OnSettingChanged", "boo");
            var eventArgs = await tcs.Task;

            Assert.Equal(eventArgs.Action, SettingChangeAction.Add);
            Assert.Equal(eventArgs.Key, "OnSettingChanged");
            Assert.True(eventArgs.Value.Equals("boo"));
        }


        [Fact]
        public void SetDefault_DoesNotRemove_SimpleType()
        {
            this.Settings.Set("Bool", false);
            Assert.True(this.Settings.Contains("Bool"), "Bool is missing");

            this.Settings.Set("Long", 0L);
            Assert.True(this.Settings.Contains("Long"), "Long is missing");
        }


        [Fact]
        public void SetDefault_DoesRemove_ComplexType()
        {
            this.Settings.Set("Object", new object());
            Assert.True(this.Settings.Contains("Object"), "Object is missing");

            this.Settings.Set<object>("Object", null);
            Assert.False(this.Settings.Contains("Object"), "Object should be missing");
        }


        [Fact]
        public void Object()
        {
            var inv = new Tuple<int, string>(1, "2");
            this.Settings.Set("Object", inv);

            var outv = this.Settings.Get<Tuple<int, string>>("Object");
            Assert.Equal(inv.Item1, outv.Item1);
            Assert.Equal(inv.Item2, outv.Item2);
        }


        [Fact]
        public void IntTest()
        {
            this.Settings.Set("Test", 99);
            var value = this.Settings.Get<int>("Test");
            Assert.True(value.Equals(99));
        }


        [Fact]
        public void IntNullTest()
        {
            var nvalue = this.Settings.Get<int?>("Blah");
            Assert.True(nvalue == null, "Int? should be null");

            nvalue = 199;
            this.Settings.Set("Blah", nvalue);

            nvalue = this.Settings.Get<int?>("Blah");
            Assert.True(nvalue.Value.Equals(199));
        }


        [Fact]
        public void DateTimeNullTest()
        {
            var dt = new DateTime(1999, 12, 31, 23, 59, 0);
            var nvalue = this.Settings.Get<DateTime?>("DateTimeNullTest");
            Assert.True(nvalue == null, "Should be null");

            this.Settings.Set("DateTimeNullTest", dt);
            nvalue = this.Settings.Get<DateTime?>("DateTimeNullTest");
            Assert.Equal(nvalue, dt);
        }


        [Fact]
        public void DateTimeOffsetTest()
        {
            var dt = DateTimeOffset.Now.TrimDate();
            this.Settings.Set("now", dt);
            var result = this.Settings.Get<DateTimeOffset>("now");
            Assert.Equal(dt, result);
        }


        [Fact]
        public void SetOverride()
        {
            this.Settings.Set("Test", "1");
            this.Settings.Set("Test", "2");
            var r = this.Settings.Get<string>("Test");
            Assert.True(r.Equals("2"));
        }


        [Fact]
        public void ContainsTest()
        {
            Assert.False(this.Settings.Contains(Guid.NewGuid().ToString()), "Contains should have returned false");

            this.Settings.Set("Test", "1");
            Assert.True(this.Settings.Contains("Test"), "Contains should have returned true");
        }


        [Fact]
        public void RemoveTest()
        {
            this.Settings.Set("Test", "1");
            Assert.True(this.Settings.Remove("Test"), "Remove should have returned success");
        }


        [Fact]
        public void LongTest()
        {
            long value = 1;
            this.Settings.Set("LongTest", value);
            var value2 = this.Settings.Get<long>("LongTest");
            Assert.Equal(value, value2);
        }


        [Fact]
        public void GuidTest()
        {
            Assert.Equal(this.Settings.Get<Guid>("GuidTest"), Guid.Empty);

            var guid = new Guid();
            this.Settings.Set("GuidTest", guid);
            Assert.Equal(this.Settings.Get<Guid>("GuidTest"), guid);
        }


        [Fact]
        public void SetNullRemoves()
        {
            this.Settings.Set("SetNullRemoves", "Blah");
            this.Settings.Set<string>("SetNullRemoves", null);
            Assert.False(this.Settings.Contains("SetNullRemoves"));
        }


        [Fact]
        public void NullBools()
        {
            this.Settings.Set<bool?>("SetNullBool", null);
            var value = this.Settings.Get<bool?>("SetNullBool");
            Assert.Null(value);

            this.Settings.Set<bool?>("SetNullBool", true);
            value = this.Settings.Get<bool?>("SetNullBool");
            Assert.Equal(value, true);

            this.Settings.Set<bool?>("SetNullBool", false);
            value = this.Settings.Get<bool?>("SetNullBool");
            Assert.Equal(value, false);

            this.Settings.Set<bool?>("SetNullBool", null);
            value = this.Settings.Get<bool?>("SetNullBool");
            Assert.Null(value);
        }


        [Fact]
        public void GetDefaultParameter()
        {
            var tmp = Guid.NewGuid().ToString();
            Assert.Equal(this.Settings.Get("GetDefaultParameter", tmp), tmp);
        }


        [Fact]
        public void TryDefaults()
        {
            Assert.True(this.Settings.SetDefault("TryDefaults", "Initial Value"), "Default value could not be set");
            Assert.False(this.Settings.SetDefault("TryDefaults", "Second Value"), "Default value was set and should not have been");
            Assert.True(this.Settings.Get<string>("TryDefaults").Equals("Initial Value"));
        }


        [Fact]
        public void ClearPreserveList()
        {
            this.Settings.Set("ClearPreserveTest", "Value");
            this.Settings.KeysNotToClear.Add("ClearPreserveTest");
            this.Settings.Clear();
            Assert.True(this.Settings.Get<string>("ClearPreserveTest").Equals("Value"));
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
        public void Binding_Basic()
        {
            var obj = this.Settings.Bind<TestBind>();
            obj.StringProperty = "Hi";

            Assert.True(this.Settings.Contains("TestBind.StringProperty"));
            Assert.False(this.Settings.Contains("TestBind.IgnoredProperty"));
            Assert.True(this.Settings.Get<string>("TestBind.StringProperty").Equals("Hi"));
        }


        [Fact]
        public void Binding_Persist()
        {
            var obj = this.Settings.Bind<TestBind>();
            obj.StringProperty = "Binding_Persist";

            var obj2 = this.Settings.Bind<TestBind>();
            Assert.True(obj.StringProperty.Equals(obj2.StringProperty));
        }
    }


    public static class Extensions
    {
        public static DateTimeOffset TrimDate(this DateTimeOffset dt)
            => new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
    }
}
using System;
using System.Collections.Generic;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;
using Shiny.Stores;
using Shiny.Testing;


namespace Shiny.Tests.Core.Stores
{
    public partial class StoreTests : IDisposable
    {
        IKeyValueStore? currentStore;


        public static IEnumerable<object[]> Data
        {
            get
            {
                var serializer = new ShinySerializer();
                IPlatform platform;
#if __ANDROID__
                platform = ShinyHost.Resolve<IPlatform>();

                yield return new object[] { new SecureKeyValueStore((AndroidPlatform)platform, (IAndroidContext)platform, serializer) };
                yield return new object[] { new SettingsKeyValueStore((IAndroidContext)platform, serializer) };
#elif __IOS__
                platform = new ApplePlatform();
                yield return new object[] { new SecureKeyValueStore(platform, serializer) };
                yield return new object[] { new SettingsKeyValueStore(serializer) };
#elif WINDOWS_UWP
                platform = new TestPlatform(); // not UwpPlatform
                //yield return new object[] { "settings" };
#else
                platform = new TestPlatform();
#endif
                yield return new object[] { new FileKeyValueStore(platform, serializer) };
                yield return new object[] { new MemoryKeyValueStore() };

                var conn = new ShinySqliteConnection(platform);
                yield return new object[] { new SqliteKeyValueStore(conn, serializer) };
            }
        }


        public void Dispose() => this.currentStore?.Clear();


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
    }
}
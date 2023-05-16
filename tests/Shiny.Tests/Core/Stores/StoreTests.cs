using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Tests.Core.Stores;


public partial class StoreTests : IDisposable
{
    IKeyValueStore? currentStore;
    readonly ITestOutputHelper output;
    public StoreTests(ITestOutputHelper output) => this.output = output;


    public static IEnumerable<object[]> Data
    {
        get
        {
            var serializer = new DefaultSerializer();
#if ANDROID
            var platform = new AndroidPlatform();
            yield return new object[] { new SecureKeyValueStore(null!, platform, serializer) };
            yield return new object[] { new SettingsKeyValueStore(platform, serializer) };
#elif IOS || MACCATALYST
            var platform = new IosPlatform();
            yield return new object[] { new SecureKeyValueStore(platform, serializer) };
            yield return new object[] { new SettingsKeyValueStore(serializer) };
#endif
            yield return new object[] { new MemoryKeyValueStore() };
        }
    }


    public void Dispose() => this.currentStore?.Clear();
}
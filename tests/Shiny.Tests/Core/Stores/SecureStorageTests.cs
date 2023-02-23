using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Tests.Core.Stores;


public class SecureStorageTests
{
    readonly IKeyValueStore secureStore;

    public SecureStorageTests()
    {
        var serializer = new DefaultSerializer();
#if ANDROID
        this.secureStore = new SecureKeyValueStore(new AndroidPlatform(), serializer);
#else
        this.secureStore = new SecureKeyValueStore(new IosPlatform(), serializer);
#endif
    }


    [Fact]
    public void RemoveWhereNotExists()
    {
        var key = Guid.NewGuid().ToString();
        this.secureStore.Clear();
        this.secureStore.Remove(key);
        this.secureStore.Remove(key);
    }


    [Fact]
    public void SetDuplicate()
    {
        var key = Guid.NewGuid().ToString();
        this.secureStore.Clear();
        this.secureStore.Set(key, "1");
        this.secureStore.Set(key, "1");
    }


    [Fact]
    public void WithBinding()
    {
        var factory = new KeyValueStoreFactory(new[] { this.secureStore });
        var logger = Shiny.Hosting.Host.Current.Logging.CreateLogger<ObjectStoreBinder>();
        var binder = new ObjectStoreBinder(factory, logger);
        var obj = new TestBind();

        try
        {
            binder.Bind(obj, this.secureStore);

            obj.StringProperty = "test";
            obj.StringProperty = null;
        }
        finally
        {
            binder.UnBind(obj);
        }
    }
}
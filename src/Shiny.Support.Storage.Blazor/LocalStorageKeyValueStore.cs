using Microsoft.JSInterop;
using Shiny.Stores;

namespace Shiny.Storage.Blazor;


/// <summary>
/// IKeyValueStore implementation backed by the browser's window.localStorage.
/// Requires <c>_content/Shiny.Support.Storage.Blazor/shiny-storage.js</c> to be loaded
/// via a &lt;script&gt; tag in the host index.html before Shiny services are used.
/// </summary>
public class LocalStorageKeyValueStore(IJSRuntime jsRuntime, ISerializer serializer) : IKeyValueStore
{
    const string KeyPrefix = "shiny:kvs:settings:";
    readonly IJSInProcessRuntime js = (IJSInProcessRuntime)jsRuntime;
    readonly ISerializer serializer = serializer;


    public bool IsReadOnly => false;


    public bool Contains(string key)
        => this.js.Invoke<bool>("shinyLocalStorage.containsKey", this.Format(key));


    public T? Get<T>(string key)
    {
        var json = this.js.Invoke<string?>("shinyLocalStorage.getItem", this.Format(key));
        if (json == null)
            return default;

        return this.serializer.Deserialize<T>(json);
    }


    public void Set<T>(string key, T value)
    {
        var json = this.serializer.Serialize<T>(value);
        this.js.InvokeVoid("shinyLocalStorage.setItem", this.Format(key), json);
    }


    public bool Remove(string key)
        => this.js.Invoke<bool>("shinyLocalStorage.removeItem", this.Format(key));


    public void Clear()
        => this.js.Invoke<int>("shinyLocalStorage.removeKeys", KeyPrefix);


    string Format(string key) => $"{KeyPrefix}{key}";
}

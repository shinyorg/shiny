using System;
using Microsoft.JSInterop;
using Shiny.Reflection;
using Shiny.Stores;

namespace Shiny.Storage.Blazor;


/// <summary>
/// IKeyValueStore implementation backed by the browser's window.localStorage.
/// Requires <c>_content/Shiny.Support.Storage.Blazor/shiny-storage.js</c> to be loaded
/// via a &lt;script&gt; tag in the host index.html before Shiny services are used.
/// </summary>
public class LocalStorageKeyValueStore : IKeyValueStore
{
    const string KeyPrefix = "shiny:kvs:";

    readonly IJSInProcessRuntime js;
    readonly ISerializer serializer;


    public LocalStorageKeyValueStore(IJSRuntime jsRuntime, ISerializer serializer)
    {
        this.js = (IJSInProcessRuntime)jsRuntime;
        this.serializer = serializer;
    }


    public string Alias => "settings";
    public bool IsReadOnly => false;


    public bool Contains(string key)
        => this.js.Invoke<bool>("shinyLocalStorage.containsKey", this.Format(key));


    public object? Get(Type type, string key)
    {
        var json = this.js.Invoke<string?>("shinyLocalStorage.getItem", this.Format(key));
        if (json == null)
            return type.GetDefaultValue();

        return this.serializer.Deserialize(type, json);
    }


    public void Set(string key, object value)
    {
        var json = this.serializer.Serialize(value);
        this.js.InvokeVoid("shinyLocalStorage.setItem", this.Format(key), json);
    }


    public bool Remove(string key)
        => this.js.Invoke<bool>("shinyLocalStorage.removeItem", this.Format(key));


    public void Clear()
        => this.js.Invoke<int>("shinyLocalStorage.removeKeys", $"{KeyPrefix}{this.Alias}:");


    string Format(string key) => $"{KeyPrefix}{this.Alias}:{key}";
}

using System;
using Shiny.Stores;
using Shiny.Infrastructure;
using System.Threading.Tasks;

namespace Shiny.Web.Stores;


public class LocalStorageStore : IKeyValueStore
{
    readonly ISerializer serializer;
    //IJSInProcessObjectReference module = null!;


    public LocalStorageStore(ISerializer serializer)
    {
        this.serializer = serializer;
    }


    //public async Task OnStart(IJSInProcessRuntime jsRuntime)
    //{
    //    this.module = await jsRuntime.ImportInProcess("Shiny.Core.Blazor", "storage.js");
    //}


    public string Alias => "settings";
    public bool IsReadOnly => false;


    public bool Remove(string key) => false; // => this.module.Invoke<bool>("remove", key);
    public void Clear() { } // => this.module.InvokeVoid("clear");
    public bool Contains(string key) => false; //=> this.module.Invoke<bool>("exists", key);


    public object Get(Type type, string key)
    {
        //var value = this.module.Invoke<string>("get", key);
        var value = "get";
        if (value == null)
            return null!;

        return this.serializer.Deserialize(type, value);
    }


    public void Set(string key, object value)
    {
        var json = this.serializer.Serialize(value);
        //this.module.Invoke<bool>("set", key, json);
        //this.module.InvokeVoid("set", key, json);
    }
}

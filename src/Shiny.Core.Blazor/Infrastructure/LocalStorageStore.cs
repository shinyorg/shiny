using Microsoft.JSInterop;
using System;
using Shiny.Stores;
using Shiny.Infrastructure;

namespace Shiny.Web.Stores;


public class LocalStorageStore : IKeyValueStore
{
    readonly IJSInProcessRuntime jsProc;
    readonly ISerializer serializer;


    public LocalStorageStore(IJSRuntime jsRuntime, ISerializer serializer)
    {
        this.jsProc = (IJSInProcessRuntime)jsRuntime;
        this.serializer = serializer;
    }


    public string Alias => "settings";
    public bool IsReadOnly => false;


    public bool Remove(string key)
    {
        if (this.Contains(key))
        {
            this.jsProc.InvokeVoid("localStorage.removeItem", key);
            return true;
        }
        return false;
    }


    public void Clear()
        => this.jsProc.InvokeVoid("localStorage.clear");


    public bool Contains(string key)
        => this.jsProc.Invoke<bool>("localStorage.hasOwnProperty", key);


    public object Get(Type type, string key)
    {
        var value = this.jsProc.Invoke<string>("localStorage.getItem", key);
        return this.serializer.Deserialize(type, value);
    }


    public void Set(string key, object value)
    {
        var data = this.serializer.Serialize(value);
        this.jsProc.InvokeVoid("localStorage.setItem", key, data);
    }
}

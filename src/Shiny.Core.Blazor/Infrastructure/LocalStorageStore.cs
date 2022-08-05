//using Microsoft.JSInterop;
//using System;
//using Shiny.Stores;

//namespace Shiny.Web.Stores;


//public class LocalStorageStore : IKeyValueStore
//{
//    readonly IJSInProcessRuntime js;
//    readonly ISerializer serializer;


//    public LocalStorageStore(IJSRuntime jsRuntime, ISerializer serializer)
//    {
//        this.js = jsRuntime as IJSInProcessRuntime;
//        this.serializer = serializer;
//    }


//    public string Alias => "settings";
//    public bool IsReadOnly => false;


//    public bool Remove(string key)
//    {
//        if (this.Contains(key))
//        {
//            this.js.InvokeVoid("localStorage.removeItem", key);
//            return true;
//        }
//        return false;
//    }


//    public void Clear()
//        => this.js.InvokeVoid("localStorage.clear");


//    public bool Contains(string key)
//        => this.js.Invoke<bool>("localStorage.hasOwnProperty", key);


//    public object Get(Type type, string key)
//    {
//        var value = this.js.Invoke<string>("localStorage.getItem", key);
//        return this.serializer.Deserialize(type, value);
//    }


//    public void Set(string key, object value)
//    {
//        var data = this.serializer.Serialize(value);
//        this.js.InvokeVoid("localStorage.setItem", key, data);
//    }
//}

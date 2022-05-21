using System;

namespace Shiny.Stores;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ObjectStoreBinderAttribute : Attribute
{
    public ObjectStoreBinderAttribute(string storeAlias) => this.StoreAlias = storeAlias;
    public string StoreAlias { get; }
}

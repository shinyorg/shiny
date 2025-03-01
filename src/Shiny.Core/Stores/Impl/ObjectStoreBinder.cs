using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Shiny.Stores.Impl;


public class ObjectStoreBinder(
    IKeyValueStoreFactory factory, 
    ILogger<ObjectStoreBinder> logger
) : IObjectStoreBinder, IDisposable
{
    readonly object syncLock = new();
    readonly Dictionary<object, IKeyValueStore> bindings = new();
    readonly List<INotifyPropertyChanged> boundObjects = new();

    public void RemovePersistedValues(INotifyPropertyChanged npc, string? keyValueStoreAlias = null)
    {
        var store = this.GetStore(npc, keyValueStoreAlias);
        this.RemovePersistedValues(npc, store);
    }


    public void RemovePersistedValues(INotifyPropertyChanged npc, IKeyValueStore store)
    {
        var type = npc.GetType();
        var props = this.GetTypeProperties(type).ToList();

        foreach (var prop in props)
        {
            var key = GetBindingKey(type, prop);
            store.Remove(key);
        }
    }


    public void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null)
    {
        var store = this.GetStore(npc, keyValueStoreAlias);
        this.Bind(npc, store ?? factory.DefaultStore);
    }


    public void Bind(INotifyPropertyChanged npc, IKeyValueStore store)
    {
        try
        {
            var type = npc.GetType();
            var props = this.GetTypeProperties(type).ToList();

            // Skip if there are no properties to bind
            if (props.Count == 0)
            {
                // logger.BindInfo("Skipped (no get/set properties)", npc.GetType()!.FullName!, store.Alias);
                return;
            }

            foreach (var prop in props)
            {
                var key = GetBindingKey(type, prop);
                if (store.Contains(key))
                {
                    var value = store.Get(prop.PropertyType, key);
                    try
                    {
                        prop.SetValue(npc, value);
                    }
                    catch (Exception ex)
                    {
                        // logger.PropertyBindError(ex, type.FullName!, prop.Name);
                    }
                }
            }
            lock (this.syncLock)
            {
                // set these before npc hook
                this.boundObjects.Add(npc);
                this.bindings.Add(npc, store);
            }

            npc.PropertyChanged += this.OnPropertyChanged;
            // logger.BindInfo("Success", npc.GetType().FullName!, store.Alias);
        }
        catch (Exception ex)
        {
            // logger.BindError(ex, npc?.GetType().FullName ?? "Unknown", store.Alias);
        }
    }


    public virtual void UnBind(INotifyPropertyChanged obj)
    {
        obj.PropertyChanged -= this.OnPropertyChanged;
        lock (this.syncLock)
            this.boundObjects.Remove(obj);
    }


    public virtual void UnBindAll()
    {
        lock (this.syncLock)
        {
            foreach (var boundObj in this.boundObjects)
                boundObj.PropertyChanged -= this.OnPropertyChanged;

            this.boundObjects.Clear();
        }
    }


    public static string GetBindingKey(Type type, PropertyInfo prop)
        => GetBindingKey(type, prop.Name);


    public static string GetBindingKey(Type type, string propertyName)
        => $"{type.Namespace}.{type.Name}.{propertyName}";


    protected virtual IKeyValueStore GetStore(INotifyPropertyChanged npc, string? keyValueStoreAlias)
    {
        IKeyValueStore? store = null;

        if (keyValueStoreAlias != null)
        {
            store = factory.GetStore(keyValueStoreAlias);
        }
        else
        {
            keyValueStoreAlias = npc
                .GetType()
                .GetCustomAttribute<ObjectStoreBinderAttribute>()?
                .StoreAlias;

            if (keyValueStoreAlias != null)
                store = factory.GetStore(keyValueStoreAlias); // error if attribute is bad
        }
        return store ?? factory.DefaultStore;
    }

    /// <summary>
    /// Get all type properties with public get and set accessors
    /// </summary>
    protected virtual IEnumerable<PropertyInfo> GetTypeProperties(Type type) => type
        .GetTypeInfo()
        .GetProperties()
        .Where(x =>
            (x.GetGetMethod() != null) &&
            (x.GetSetMethod() != null)
        );


    protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender == null)
        {
            logger.LogDebug("Null sender");
            return;
        }
        var prop = this
            .GetTypeProperties(sender.GetType())
            .FirstOrDefault(x => x.Name.Equals(args.PropertyName));

        if (prop != null)
        {
            var key = GetBindingKey(sender.GetType(), prop);
            var value = prop.GetValue(sender);

            lock (this.syncLock)
            {
                if (!this.bindings.ContainsKey(sender))
                    throw new ArgumentException("No key/value store found for current binding object - " + sender.GetType().FullName);

                this.bindings[sender].SetOrRemove(key, value);
            }
        }
    }

    public void Dispose() => this.UnBindAll();
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Shiny.Stores
{
    public interface IObjectStoreBinder
    {
        /// <summary>
        /// Attempts to bind an object to a named store, if the alias is not passed, the binder will look at the attribute
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="keyValueStoreAlias"></param>
        void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null);


        /// <summary>
        /// Binds an object to a given store
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="store"></param>
        void Bind(INotifyPropertyChanged npc, IKeyValueStore store);


        /// <summary>
        /// Unbinds an object from whatever store it was bound to
        /// </summary>
        /// <param name="npc"></param>
        void UnBind(INotifyPropertyChanged npc);
    }


    public class ObjectStoreBinder : IObjectStoreBinder
    {
        readonly IDictionary<object, IKeyValueStore> bindings;
        readonly IKeyValueStoreFactory factory;


        public ObjectStoreBinder(IKeyValueStoreFactory factory)
        {
            this.factory = factory;
            this.bindings = new Dictionary<object, IKeyValueStore>();
        }


        public void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null)
        {
            IKeyValueStore? store = null;

            if (keyValueStoreAlias != null)
            {
                store = this.factory.GetStore(keyValueStoreAlias);
            }
            else
            {
                keyValueStoreAlias = npc.GetType().GetCustomAttribute<ObjectStoreBinderAttribute>()?.StoreAlias;
                if (keyValueStoreAlias != null)
                    store = this.factory.GetStore(keyValueStoreAlias); // error if attribute is bad
            }

            this.Bind(npc, store ?? this.factory.DefaultStore);
        }


        public void Bind(INotifyPropertyChanged npc, IKeyValueStore store)
        {
            var type = npc.GetType();
            var props = this.GetTypeProperties(type);

            foreach (var prop in props)
            {
                var key = GetBindingKey(type, prop);
                if (store.Contains(key))
                {
                    var value = store.Get(prop.PropertyType, key);
                    prop.SetValue(npc, value);
                }
            }
            npc.PropertyChanged += this.OnPropertyChanged;
            this.bindings.Add(npc, store);
        }


        public virtual void UnBind(INotifyPropertyChanged obj)
            => obj.PropertyChanged -= this.OnPropertyChanged;


        public static string GetBindingKey(Type type, PropertyInfo prop)
            => GetBindingKey(type, prop.Name);


        public static string GetBindingKey(Type type, string propertyName)
            => $"{type.Namespace}.{type.Name}.{propertyName}";


        protected virtual IEnumerable<PropertyInfo> GetTypeProperties(Type type) => type
            .GetTypeInfo()
            .DeclaredProperties
            .Where(x =>
                x.CanRead &&
                x.CanWrite
            );


        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            var prop = this
                .GetTypeProperties(sender.GetType())
                .FirstOrDefault(x => x.Name.Equals(args.PropertyName));

            if (prop != null)
            {
                var key = GetBindingKey(sender.GetType(), prop);
                var value = prop.GetValue(sender);

                if (!this.bindings.ContainsKey(sender))
                    throw new ArgumentException("No key/value store found for current binding object - " + sender.GetType().FullName);

                this.bindings[sender].SetOrRemove(key, value);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Shiny.Stores
{
    public interface IObjectStoreBinder
    {
        void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null);
        void Bind(INotifyPropertyChanged npc, IKeyValueStore store);
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
            => this.Bind(
                npc,
                keyValueStoreAlias == null
                    ? this.factory.DefaultStore
                    : this.factory.GetStore(keyValueStoreAlias)
            );


        public void Bind(INotifyPropertyChanged npc, IKeyValueStore store)
        {
            var type = npc.GetType();
            var props = this.GetTypeProperties(type);

            foreach (var prop in props)
            {
                var key = this.GetBindingKey(type, prop);
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


        protected virtual string GetBindingKey(Type type, PropertyInfo prop)
            => $"{type.FullName}.{prop.Name}";


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
                var key = this.GetBindingKey(sender.GetType(), prop);
                var value = prop.GetValue(sender);

                if (!this.bindings.ContainsKey(sender))
                    throw new ArgumentException("No key/value store found for current binding object - " + sender.GetType().FullName);

                this.bindings[sender].Set(key, value);
            }
        }
    }
}

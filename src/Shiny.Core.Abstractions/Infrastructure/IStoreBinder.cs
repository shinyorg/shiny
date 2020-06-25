using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Shiny.Infrastructure
{
    public interface IStoreBinder
    {
        void Bind(IStore valueProvider, INotifyPropertyChanged obj);
        void UnBind(INotifyPropertyChanged obj);
    }


    public class StoreBinder : IStoreBinder
    {
        public void Bind(IStore valueProvider, INotifyPropertyChanged obj)
        {
            var type = obj.GetType();
            var props = this.GetTypeProperties(type);

            foreach (var prop in props)
            {
                var key = this.GetBindingKey(type, prop);
                if (valueProvider.Contains(key))
                {
                    var value = valueProvider.Get(prop.PropertyType, key);
                    prop.SetValue(obj, value);
                }
            }
            obj.PropertyChanged += this.OnPropertyChanged;
        }


        public virtual void UnBind(INotifyPropertyChanged obj)
            => obj.PropertyChanged -= this.OnPropertyChanged;


        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            var prop = this
                .GetTypeProperties(sender.GetType())
                .FirstOrDefault(x => x.Name.Equals(args.PropertyName));

            if (prop != null)
            {
                var key = this.GetBindingKey(sender.GetType(), prop);
                var value = prop.GetValue(sender);
                //this.SetValue(key, value);
            }
        }


        protected virtual string GetBindingKey(Type type, PropertyInfo prop)
            => $"{type.FullName}.{prop.Name}";


        protected virtual IEnumerable<PropertyInfo> GetTypeProperties(Type type) => type
            .GetTypeInfo()
            .DeclaredProperties
            .Where(x =>
                x.CanRead &&
                x.CanWrite
            );
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Shiny.Infrastructure;


namespace Shiny.Settings
{

    public abstract class AbstractSettings : ISettings
    {
        readonly ISerializer serializer;


        protected AbstractSettings(ISerializer serializer)
        {
            this.serializer = serializer;
            this.KeysNotToClear = new List<string>();
        }


        public abstract bool Contains(string key);
        protected abstract object NativeGet(Type type, string key);
        protected abstract void NativeSet(Type type, string key, object value);
        protected abstract void NativeRemove(string[] keys);
        protected abstract IDictionary<string, string> NativeValues();


        public event EventHandler<SettingChangeEventArgs> Changed;

        public List<string> KeysNotToClear { get; set; }
        public virtual IReadOnlyDictionary<string, string> List { get; protected set; }


        public virtual object GetValue(Type type, string key, object defaultValue = null)
        {
            try
            {
                if (!this.Contains(key))
                    return defaultValue;

                var actualType = this.UnwrapType(type);
                var value = this.NativeGet(actualType, key);
                return value;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error getting key: {key}", ex);
            }
        }


        public virtual T Get<T>(string key, T defaultValue = default)
        {
            try
            {
                if (!this.Contains(key))
                    return defaultValue;

                var type = this.UnwrapType(typeof(T));
                var value = this.NativeGet(type, key);
                return (T)value;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error getting key: {key}", ex);
            }
        }


        public virtual T GetRequired<T>(string key)
        {
            if (!this.Contains(key))
                throw new ArgumentException($"Settings key '{key}' is not set");

            return this.Get<T>(key);
        }


        public virtual void SetValue(string key, object value)
        {
            try
            {
                if (value == null)
                {
                    this.Remove(key);
                }
                else
                {
                    var action = this.Contains(key)
                        ? SettingChangeAction.Update
                        : SettingChangeAction.Add;

                    var type = this.UnwrapType(value.GetType());
                    this.NativeSet(type, key, value);
                    this.OnChanged(new SettingChangeEventArgs(action, key, value));
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error setting key {key} with value {value}", ex);
            }
        }


        public virtual void Set<T>(string key, T value)
        {
            if (key == null)
                throw new ArgumentException("Key is null");

            try
            {
                var isDefault = EqualityComparer<T>.Default.Equals(value, default);
                var action = this.Contains(key)
                    ? SettingChangeAction.Update
                    : SettingChangeAction.Add;

                if (isDefault)
                {
                    if (typeof(T).IsNullable())
                        this.Remove(key);
                    else
                    {
                        var type = this.UnwrapType(typeof(T));
                        this.NativeSet(type, key, value);
                        this.OnChanged(new SettingChangeEventArgs(action, key, value));
                    }
                }
                else
                {
                    var type = this.UnwrapType(typeof(T));
                    this.NativeSet(type, key, value);
                    this.OnChanged(new SettingChangeEventArgs(action, key, value));
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error setting key {key} with value {value}", ex);
            }
        }


        public virtual bool SetDefault<T>(string key, T value)
        {
            if (this.Contains(key))
                return false;

            var type = this.UnwrapType(typeof(T));
            this.NativeSet(type, key, value);
            return true;
        }


        public virtual bool Remove(string key)
        {
            if (!this.Contains(key))
                return false;

            this.NativeRemove(new[] { key });
            return true;
        }


        public virtual void Clear()
        {
            var keys = this.NativeValues()
                .Where(x => this.ShouldClear(x.Key))
                .Select(x => x.Key)
                .ToArray();

            this.NativeRemove(keys);
            this.OnChanged(new SettingChangeEventArgs(SettingChangeAction.Clear, null, null));
        }


        protected virtual void OnChanged(SettingChangeEventArgs args)
        {
            this.Changed?.Invoke(this, args);
            var native = this.NativeValues();
            this.List = new ReadOnlyDictionary<string, string>(native);
        }


        protected virtual string Serialize(Type type, object value)
        {
            if (type == typeof(string))
                return (string)value;

            if (this.IsStringifyType(type))
            {
                var format = value as IFormattable;
                return format == null
                    ? value.ToString()
                    : format.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            }

            return this.serializer.Serialize(value);
        }


        protected virtual object Deserialize(Type type, string value)
        {
            if (type == typeof(string))
                return value;

            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value);

            if (this.IsStringifyType(type))
                return Convert.ChangeType(value, type, System.Globalization.CultureInfo.InvariantCulture);

            return this.serializer.Deserialize(type, value);
        }


        protected virtual bool ShouldClear(string key)
            => !this.KeysNotToClear.Any(x => x.Equals(key));


        protected virtual Type UnwrapType(Type type)
        {
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type);

            return type;
        }


        protected virtual bool IsStringifyType(Type t) =>
            t == typeof(DateTime) ||
            t == typeof(DateTimeOffset) ||
            t == typeof(bool) ||
            t == typeof(short) ||
            t == typeof(int) ||
            t == typeof(long) ||
            t == typeof(double) ||
            t == typeof(float) ||
            t == typeof(decimal);


        public virtual T Bind<T>(string prefix = null) where T : INotifyPropertyChanged, new()
        {
            var obj = new T();
            this.Bind(obj, prefix);
            return obj;
        }


        public virtual void Bind(INotifyPropertyChanged obj, string prefix = null)
        {
            var type = obj.GetType();
            var prefixValue = prefix ?? type.Name + ".";
            var props = this.GetTypeProperties(type);

            foreach (var prop in props)
            {
                var key = prefixValue + prop.Name;
                if (this.Contains(key))
                {
                    var value = this.GetValue(prop.PropertyType, key);
                    prop.SetValue(obj, value);
                }
            }
            obj.PropertyChanged += this.OnPropertyChanged;
        }


        public virtual void UnBind(INotifyPropertyChanged obj)
            => obj.PropertyChanged -= this.OnPropertyChanged;


        protected virtual IEnumerable<PropertyInfo> GetTypeProperties(Type type) => type
            .GetTypeInfo()
            .DeclaredProperties
            .Where(x =>
                x.CanRead &&
                x.CanWrite
                //x.GetCustomAttribute<IgnoreAttribute>() == null
            );


        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            var prop = this
                .GetTypeProperties(sender.GetType())
                .FirstOrDefault(x => x.Name.Equals(args.PropertyName));

            if (prop != null)
            {
                var key = $"{sender.GetType().Name}.{prop.Name}";
                var value = prop.GetValue(sender);
                this.SetValue(key, value);
            }
        }
    }
}
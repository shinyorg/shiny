using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Shiny.Infrastructure;


namespace Shiny.Settings
{
    public abstract class AbstractSettings : ISettings
    {
        readonly ISerializer serializer;
        readonly Subject<SettingChange> changedSubject;


        protected AbstractSettings(ISerializer serializer)
        {
            this.changedSubject = new Subject<SettingChange>();
            this.serializer = serializer;
        }


        public abstract bool Contains(string key);
        protected abstract object NativeGet(Type type, string key);
        protected abstract void NativeSet(Type type, string key, object value);
        protected abstract void NativeRemove(string[] keys);
        protected abstract void NativeClear();
        public IObservable<SettingChange> Changed => this.changedSubject;


        public virtual object? GetValue(Type type, string key, object? defaultValue = null)
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
                if (type.IsEnum)
                    value = Enum.ToObject(type, value);
                    //value = Enum.Parse(type, value);

                return (T)value;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error getting key: {key}", ex);
            }
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
                    //if (type.IsEnum)
                    //    value = value.ToString();

                    this.NativeSet(type, key, value);
                    this.OnChanged(new SettingChange(action, key, value));
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

            if (value == null)
            {
                this.Remove(key);
                return;
            }
            try
            {
                var isDefault = EqualityComparer<T>.Default.Equals(value, default);
                var action = this.Contains(key)
                    ? SettingChangeAction.Update
                    : SettingChangeAction.Add;

                if (isDefault)
                {
                    if (typeof(T).IsNullable())
                    {
                        this.Remove(key);
                    }
                    else
                    {
                        var type = this.UnwrapType(typeof(T));
                        this.NativeSet(type, key, value);
                        this.OnChanged(new SettingChange(action, key, value));
                    }
                }
                else
                {
                    var type = this.UnwrapType(typeof(T));
                    this.NativeSet(type, key, value);
                    this.OnChanged(new SettingChange(action, key, value));
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error setting key {key} with value {value}", ex);
            }
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
            this.NativeClear();
            this.OnChanged(new SettingChange(SettingChangeAction.Clear, null, null));
        }


        protected virtual void OnChanged(SettingChange args)
            => this.changedSubject.OnNext(args);


        protected virtual string Serialize(Type type, object value)
        {
            if (type == typeof(string))
                return (string)value;

            if (this.IsStringifyType(type))
            {
                var format = value as IFormattable;
                return format == null
                    ? value.ToString()
                    : format.ToString(null, CultureInfo.InvariantCulture);
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


        protected virtual string GetBindingKey(Type type, PropertyInfo prop)
            => $"{type.FullName}.{prop.Name}";

        public virtual T Bind<T>() where T : INotifyPropertyChanged, new()
        {
            var obj = new T();
            this.Bind(obj);
            return obj;
        }


        public virtual void Bind(INotifyPropertyChanged obj)
        {
            var type = obj.GetType();
            var props = this.GetTypeProperties(type);

            foreach (var prop in props)
            {
                var key = this.GetBindingKey(type, prop);
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
                this.SetValue(key, value);
            }
        }
    }
}
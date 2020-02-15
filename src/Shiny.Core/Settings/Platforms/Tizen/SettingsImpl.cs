using System;
using System.Collections.Generic;
using System.Linq;
using Shiny.Infrastructure;
using Tizen.Applications;


namespace Shiny.Settings
{
    public class SettingsImpl : AbstractSettings
    {
        public SettingsImpl(ISerializer serializer) : base(serializer) { }


        public override bool Contains(string key)
            => Preference.Contains(key);


        public override void Clear()
        {
            base.Clear();
            Preference.RemoveAll();
        }


        public override T Get<T>(string key, T defaultValue = default)
        {
            if (!Preference.Contains(key))
                return defaultValue;

            return Preference.Get<T>(key);
        }


        protected override object NativeGet(Type type, string key)
        {
            //type = type.Unwrap();
            if (type == typeof(string))
                return Preference.Get<string>(key);

            if (type == typeof(long))
                return Preference.Get<long>(key);

            if (type == typeof(int))
                return Preference.Get<int>(key);

            if (type == typeof(bool))
                return Preference.Get<bool>(key);

            throw new ArgumentException("Invalid Type - " + type.FullName);
        }


        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                if (Preference.Contains(key))
                    Preference.Remove(key);
        }


        protected override void NativeSet(Type type, string key, object value)
            => Preference.Set(key, value);


        protected override IDictionary<string, string> NativeValues()
        {
            var dict = new Dictionary<string, string>(Preference.Keys.Count());
            foreach (var key in Preference.Keys)
            {
                var value = Preference.Get<object>(key);
                dict.Add(key, value.ToString());
            }
            return dict;
        }
    }
}

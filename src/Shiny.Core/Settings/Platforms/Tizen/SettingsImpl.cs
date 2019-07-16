using System;
using System.Collections.Generic;
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
            => throw new NotImplementedException();


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
            throw new NotImplementedException();
            //Preference.Keys
        }
    }
}

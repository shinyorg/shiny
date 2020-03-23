using System;
using Shiny.Infrastructure;
using Shiny.Settings;
using Xamarin.Essentials;


namespace Shiny.Integrations.XamEssentials
{
    public class SettingsImpl : AbstractSettings
    {
        public SettingsImpl(ISerializer serializer) : base(serializer)
        {
        }


        public override bool Contains(string key) => Preferences.ContainsKey(key);

        protected override void NativeClear()
            => Preferences.Clear();


        protected override object NativeGet(Type type, string key)
        {
            if (type == typeof(string))
                return Preferences.Get(key, (string)null);



            throw new ArgumentException("Invalid type");
        }


        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                Preferences.Remove(key);
        }


        protected override void NativeSet(Type type, string key, object value)
        {
            if (type == typeof(string))
                Preferences.Set(key, (string)value);
        }
    }
}

using System;
using System.Collections.Generic;
using Acr.Infrastructure;
using Microsoft.JSInterop;


namespace Acr.Settings
{
    public class SettingsImpl : AbstractSettings
    {
        public SettingsImpl() : base(new BlazorJsonSerializer()) {}
        public override bool Contains(string key)
            => JSRuntime.Current.InvokeAsync<bool>("AcrSettings.contains").Result;


        protected override object NativeGet(Type type, string key)
            => JSRuntime.Current.InvokeAsync<object>("AcrSettings.get", key);


        protected override void NativeSet(Type type, string key, object value)
            => JSRuntime.Current.InvokeAsync<object>("AcrSettings.set", key, value);


        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                JSRuntime.Current.InvokeAsync<bool>("Acr.Settings.Remove", key);
        }


        protected override IDictionary<string, string> NativeValues()
        {
            var items = JSRuntime.Current.InvokeAsync<Dictionary<string, string>>("AcrSettings.list").Result;
            return items;
        }
    }
}

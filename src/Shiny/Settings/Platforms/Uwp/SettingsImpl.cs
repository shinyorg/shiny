using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Windows.Storage;
using Windows.System;


namespace Shiny.Settings
{
    public class SettingsImpl : AbstractSettings
    {
        readonly ApplicationDataContainer container;


        public SettingsImpl(ISerializer serializer, bool isRoaming = false) : base(serializer)
        {
            this.container = isRoaming
                ? ApplicationData.Current.RoamingSettings
                : ApplicationData.Current.LocalSettings;
        }


        public override async Task<bool> OpenAppSettings()
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures-app"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Contains(string key) => this.container.Values.ContainsKey(key);


        protected override object NativeGet(Type type, string key)
        {
            var @string = (string)this.container.Values[key];
            var @object = this.Deserialize(type, @string);
            return @object;
        }


        protected override void NativeSet(Type type, string key, object value)
        {
            var @string = this.Serialize(type, value);
            this.container.Values[key] = @string;
        }


        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                this.container.Values.Remove(key);
        }


        protected override void NativeClear()
            => this.container.Values.Clear();
    }
}

using System;
using System.Collections.Generic;


namespace Shiny.Generators
{
    internal static class Constants
    {
        public static readonly string ShinyApplicationAttributeTypeName = "Shiny.ShinyApplicationAttribute";


        public static Dictionary<string, string> IosThirdPartyRegistrations = new Dictionary<string, string>()
        {
            // Xamarin Forms Material
            { "Xamarin.Forms.FormsMaterial", "global::Xamarin.Forms.FormsMaterial.Init();" },

            // AiForms.SettingsView
            { "AiForms.Renderers.iOS.SettingsViewInit", "global::AiForms.Renderers.iOS.SettingsViewInit.Init();" },

            // XF Material
            { "XF.Material.iOS.Material", "global::XF.Material.iOS.Material.Init();" },

            // RG Plugins
            { "Rg.Plugins.Popup.Popup", "Rg.Plugins.Popup.Popup.Init();" },

            // FF Image Loading
            { "FFImageLoading.Forms.Platform.CachedImageRenderer", "FFImageLoading.Forms.Platform.CachedImageRenderer.Init();" }
        };


        public static Dictionary<string, string> AndroidActivityThirdPartyRegistrations = new Dictionary<string, string>()
        {
            // AiForms SettingsView
            { "AiForms.Renderers.Droid.SettingsViewInit", "global::AiForms.Renderers.Droid.SettingsViewInit.Init();" },

            // XF Material
            { "XF.Material.Forms.Material", "global::XF.Material.Droid.Material.Init(this, savedInstanceState);" },

            // RG Plugins
            { "Rg.Plugins.Popup.Popup", "global::Rg.Plugins.Popup.Popup.Init(this);" },

            // FF Image Loading
            { "FFImageLoading.Forms.Platform.CachedImageRenderer", "FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);" }
        };


        public static Dictionary<string, string> AndroidApplicationThirdPartyRegistrations = new Dictionary<string, string>()
        {
            { "Acr.UserDialogs.UserDialogs", "global::Acr.UserDialogs.UserDialogs.Init(this);" }
        };
    }
}

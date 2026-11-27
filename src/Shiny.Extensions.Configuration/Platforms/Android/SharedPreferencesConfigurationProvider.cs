using System.Collections.Generic;
using Android.App;
using AndroidX.Preference;
using Microsoft.Extensions.Configuration;

namespace Shiny.Extensions.Configuration;


public class SharedPreferencesConfigurationProvider : ConfigurationProvider
{
    // SharedPreferences only holds its change listeners weakly - this field is what keeps it alive
    PrefChangeListener? listener;


    public override void Load()
    {
        if (this.listener == null)
        {
            this.listener = new PrefChangeListener(() =>
            {
                // OnReload only raises the change token - the data has to be re-read first. A fresh dictionary rather
                // than Clear, so a reader enumerating the old one on another thread doesn't throw
                this.Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                this.DoLoad();
                this.OnReload();
            });
            PreferenceManager
                .GetDefaultSharedPreferences(Application.Context)!
                .RegisterOnSharedPreferenceChangeListener(this.listener);
        }

        // Load runs again on IConfigurationRoot.Reload()
        this.Data.Clear();
        this.DoLoad();
        base.Load();
    }


    public override void Set(string key, string value)
    {
        using (var prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context))
        {
            using (var editor = prefs!.Edit()!)
            {
                editor.PutString(key, value);
                editor.Apply();
            }
        }
        base.Set(key, value);
    }


    protected virtual void DoLoad()
    {
        using (var prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context))
        {
            if (prefs?.All != null)
            {
                // the indexer, not Add - configuration keys are case-insensitive and preference keys are not
                foreach (var pair in prefs.All)
                    this.Data[pair.Key] = pair.Value?.ToString();
            }
        }
    }
}

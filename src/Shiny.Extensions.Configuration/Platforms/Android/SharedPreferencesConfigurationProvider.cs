using Android.App;
using AndroidX.Preference;
using Microsoft.Extensions.Configuration;

namespace Shiny.Extensions.Configuration;


public class SharedPreferencesConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        // do I have to also trigger the load?
        // do I have to populate data?
        PreferenceManager
            .GetDefaultSharedPreferences(Application.Context)!
            .RegisterOnSharedPreferenceChangeListener(new PrefChangeListener(() => this.OnReload()));

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
                foreach (var pair in prefs.All)
                {
                    this.Data.Add(pair.Key, pair.Value.ToString());
                }
            }
        }
    }
}
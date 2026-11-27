using Foundation;
using Microsoft.Extensions.Configuration;

namespace Shiny.Extensions.Configuration;


public class NSUserDefaultsConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        // Load runs again on IConfigurationRoot.Reload()
        this.Data.Clear();
        this.DoLoad();
        base.Load();
    }


    public override void Set(string key, string value)
    {
        using var native = NSUserDefaults.StandardUserDefaults;
        native.SetString(value, key);
        native.Synchronize();
        
        base.Set(key, value);
        this.OnReload();
    }


    protected virtual void DoLoad()
    {
        using var native = NSUserDefaults.StandardUserDefaults;
        var dict = native.ToDictionary();

        foreach (var pair in dict)
        {
            var key = pair.Key.ToString();
            var value = pair.Value?.ToString();

            // the indexer, not Add - configuration keys are case-insensitive and defaults keys are not
            this.Data[key] = value;
        }
    }
}
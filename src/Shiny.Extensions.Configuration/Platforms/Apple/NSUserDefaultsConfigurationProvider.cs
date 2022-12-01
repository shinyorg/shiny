using Foundation;
using Microsoft.Extensions.Configuration;

namespace Shiny.Extensions.Configuration;


public class NSUserDefaultsConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
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
            var value = pair.Value.ToString();
            this.Data.Add(key, value);
        }
    }
}
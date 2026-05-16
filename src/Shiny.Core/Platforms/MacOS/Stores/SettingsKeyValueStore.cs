using System;
using Foundation;

namespace Shiny.Stores;


public class SettingsKeyValueStore(ISerializer serializer) : IKeyValueStore
{
    public bool IsReadOnly => false;


    public void Clear() => this.Do(prefs =>
    {
        foreach (var key in prefs.ToDictionary())
            prefs.RemoveObject(key.Key.ToString());
    });


    public bool Contains(string key)
        => this.GetValue(false, x => x.ValueForKey(new NSString(key)) != null);


    public T? Get<T>(string key) => this.GetValue(default(T), prefs =>
    {
        if (prefs.ValueForKey(new NSString(key)) == null)
            return default;

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => (T)(object)prefs.BoolForKey(key),
            TypeCode.Double  => (T)(object)prefs.DoubleForKey(key),
            TypeCode.Int32   => (T)(object)(int)prefs.IntForKey(key),
            TypeCode.Single  => (T)(object)prefs.FloatForKey(key),
            TypeCode.String  => (T)(object)(prefs.StringForKey(key) ?? string.Empty),
            _                => serializer.Deserialize<T>(prefs.StringForKey(key) ?? string.Empty)
        };
    });


    public bool Remove(string key) => this.GetValue(false, prefs =>
    {
        if (prefs.ValueForKey(new NSString(key)) == null)
            return false;

        prefs.RemoveObject(key);
        return true;
    });


    public void Set<T>(string key, T value) => this.Do(prefs =>
    {
        switch (value)
        {
            case bool b:   prefs.SetBool(b, key); break;
            case double d: prefs.SetDouble(d, key); break;
            case int i:    prefs.SetInt(i, key); break;
            case string s: prefs.SetString(s, key); break;
            default:
                prefs.SetString(serializer.Serialize<T>(value), key);
                break;
        }
    });


    readonly object syncLock = new();

    protected virtual T GetValue<T>(T defaultValue, Func<NSUserDefaults, T> getter)
    {
        lock (this.syncLock)
        {
            using var native = NSUserDefaults.StandardUserDefaults;
            return getter(native);
        }
    }

    protected virtual void Do(Action<NSUserDefaults> action)
    {
        lock (this.syncLock)
        {
            using var native = NSUserDefaults.StandardUserDefaults;
            action(native);
            native.Synchronize();
        }
    }
}

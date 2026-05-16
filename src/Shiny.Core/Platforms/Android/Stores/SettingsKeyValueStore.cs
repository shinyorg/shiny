using System;
using Android.Content;

namespace Shiny.Stores;


public class SettingsKeyValueStore(AndroidPlatform platform, ISerializer serializer) : IKeyValueStore
{
    public bool IsReadOnly => false;
    public void Clear() => this.Do((_, edit) => edit.Clear());
    public bool Contains(string key)
    {
        lock (this.syncLock)
            return this.GetPrefs().Contains(key);
    }


    public T? Get<T>(string key)
    {
        lock (this.syncLock)
        {
            using var prefs = this.GetPrefs();
            if (!prefs.Contains(key))
                return default;

            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Boolean => (T)(object)prefs.GetBoolean(key, false),
                TypeCode.Int32   => (T)(object)prefs.GetInt(key, 0),
                TypeCode.Int64   => (T)(object)prefs.GetLong(key, 0L),
                TypeCode.Single  => (T)(object)prefs.GetFloat(key, 0f),
                TypeCode.String  => (T)(object)(prefs.GetString(key, String.Empty) ?? String.Empty),
                _                => serializer.Deserialize<T>(prefs.GetString(key, String.Empty)!)
            };
        }
    }


    public bool Remove(string key)
    {
        var removed = false;
        this.Do((prefs, edit) =>
        {
            if (prefs.Contains(key))
            {
                edit.Remove(key);
                removed = true;
            }
        });
        return removed;
    }


    public void Set<T>(string key, T value) => this.Do((prefs, edit) =>
    {
        switch (value)
        {
            case bool b:   edit.PutBoolean(key, b); break;
            case int i:    edit.PutInt(key, i); break;
            case long l:   edit.PutLong(key, l); break;
            case float f:  edit.PutFloat(key, f); break;
            case string s: edit.PutString(key, s); break;
            default:
                edit.PutString(key, serializer.Serialize<T>(value));
                break;
        }
    });


    readonly object syncLock = new();
    void Do(Action<ISharedPreferences, ISharedPreferencesEditor> doWork)
    {
        lock (this.syncLock)
        {
            using var prefs = this.GetPrefs();
            using var editor = prefs.Edit()!;

            doWork(prefs, editor);
            editor.Commit();
        }
    }


    protected ISharedPreferences GetPrefs()
        => platform.AppContext.GetSharedPreferences("Shiny", FileCreationMode.Private)!;
}

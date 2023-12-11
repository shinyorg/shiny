using System;
using Android.Content;
using Shiny.Reflection;

namespace Shiny.Stores;


public class SettingsKeyValueStore : IKeyValueStore
{
    readonly AndroidPlatform platform;
    readonly ISerializer serializer;


    public SettingsKeyValueStore(AndroidPlatform platform, ISerializer serializer)
    {
        this.platform = platform;
        this.serializer = serializer;
    }


    public string Alias => "settings";
    public bool IsReadOnly => false;
    public void Clear() => this.Do((_, edit) => edit.Clear());
    public bool Contains(string key)
    {
        lock (this.syncLock)
            return this.GetPrefs().Contains(key);
    }


    public object? Get(Type type, string key)
    {
        lock (this.syncLock)
        {
            using var prefs = this.GetPrefs();
            if (!prefs.Contains(key))
                return type.GetDefaultValue();

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => prefs.GetBoolean(key, false),
                TypeCode.Int32 => prefs.GetInt(key, 0),
                TypeCode.Int64 => prefs.GetLong(key, 0),
                TypeCode.Single => prefs.GetFloat(key, 0),
                TypeCode.String => prefs.GetString(key, String.Empty),
                _ => this.serializer.Deserialize(type, prefs.GetString(key, String.Empty))
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


    public void Set(string key, object value) => this.Do((prefs, edit) =>
    {
        var typeCode = Type.GetTypeCode(value.GetType());
        switch (typeCode)
        {
            case TypeCode.Boolean:
                edit.PutBoolean(key, (bool)value);
                break;

            case TypeCode.Int32:
                edit.PutInt(key, (int)value);
                break;

            case TypeCode.Int64:
                edit.PutLong(key, (long)value);
                break;

            case TypeCode.Single:
                edit.PutFloat(key, (float)value);
                break;

            case TypeCode.String:
                edit.PutString(key, (string)value);
                break;

            default:
                var @string = this.serializer.Serialize(value);
                edit.PutString(key, @string);
                break;
        }
    });


    readonly object syncLock = new object();
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
        => this.platform.AppContext.GetSharedPreferences("Shiny", FileCreationMode.Private)!;
}
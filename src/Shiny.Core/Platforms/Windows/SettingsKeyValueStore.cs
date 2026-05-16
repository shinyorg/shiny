using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;

namespace Shiny.Stores;


public class SettingsKeyValueStore : IKeyValueStore
{
    public string? ContainerName { get; set; }
    readonly ISerializer serializer;


    public SettingsKeyValueStore(ISerializer serializer)
        => this.serializer = serializer;


    public string Alias => "settings";
    public bool IsReadOnly => false;

    public void Clear()
    {
        if (this.IsPackaged)
        {
            this.Container.Values.Clear();
        }
        else
        {
            this.EnsureFileLoaded();
            this.fileValues!.Clear();
            this.SaveFile();
        }
    }

    public bool Contains(string key)
    {
        if (this.IsPackaged)
            return this.Container.Values.ContainsKey(key);

        this.EnsureFileLoaded();
        return this.fileValues!.ContainsKey(key);
    }

    public object? Get(Type type, string key)
    {
        if (!this.Contains(key))
            return null;

        var value = this.IsPackaged
            ? (string)this.Container.Values[key]
            : this.fileValues![key];

        return this.serializer.Deserialize(type, value);
    }

    public bool Remove(string key)
    {
        if (this.IsPackaged)
            return this.Container.Values.Remove(key);

        this.EnsureFileLoaded();
        var removed = this.fileValues!.Remove(key);
        if (removed)
            this.SaveFile();
        return removed;
    }

    public void Set(string key, object value)
    {
        var s = this.serializer.Serialize(value);
        if (this.IsPackaged)
        {
            if (this.Container.Values.ContainsKey(key))
                this.Container.Values[key] = s;
            else
                this.Container.Values.Add(key, s);
        }
        else
        {
            this.EnsureFileLoaded();
            this.fileValues![key] = s;
            this.SaveFile();
        }
    }


    // ---- Packaged backing (WinRT LocalSettings) ----
    ApplicationDataContainer? container;
    protected virtual ApplicationDataContainer Container
    {
        get
        {
            this.ContainerName ??= "shiny";
            this.container ??= ApplicationData.Current.LocalSettings.CreateContainer(this.ContainerName, ApplicationDataCreateDisposition.Always);
            return this.container;
        }
    }


    // ---- Unpackaged fallback (JSON file in %LOCALAPPDATA%) ----
    static bool? isPackaged;
    bool IsPackaged
    {
        get
        {
            if (isPackaged == null)
            {
                try
                {
                    _ = ApplicationData.Current.LocalSettings;
                    isPackaged = true;
                }
                catch (InvalidOperationException)
                {
                    isPackaged = false;
                }
            }
            return isPackaged.Value;
        }
    }

    Dictionary<string, string>? fileValues;
    string? filePath;

    void EnsureFileLoaded()
    {
        if (this.fileValues != null)
            return;

        this.ContainerName ??= "shiny";
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDomain.CurrentDomain.FriendlyName,
            "Shiny"
        );
        Directory.CreateDirectory(dir);
        this.filePath = Path.Combine(dir, this.ContainerName + ".json");

        if (File.Exists(this.filePath))
        {
            try
            {
                var json = File.ReadAllText(this.filePath);
                this.fileValues = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.DictionaryStringString) ?? new Dictionary<string, string>();
            }
            catch
            {
                this.fileValues = new Dictionary<string, string>();
            }
        }
        else
        {
            this.fileValues = new Dictionary<string, string>();
        }
    }

    void SaveFile()
    {
        if (this.filePath == null || this.fileValues == null)
            return;
        File.WriteAllText(this.filePath, JsonSerializer.Serialize(this.fileValues, SettingsJsonContext.Default.DictionaryStringString));
    }
}


[JsonSerializable(typeof(Dictionary<string, string>))]
sealed partial class SettingsJsonContext : JsonSerializerContext { }

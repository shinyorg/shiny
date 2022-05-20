using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny.Stores;


public interface IKeyValueStoreFactory
{
    string[] AvailableStores { get; }
    bool HasStore(string aliasName);
    IKeyValueStore GetStore(string aliasName);
    IKeyValueStore DefaultStore { get; }
    void SetDefaultStore(string aliasName);
}


public class KeyValueStoreFactory : IKeyValueStoreFactory
{
    public static string DefaultStoreName { get; set; } = "settings";


    readonly IEnumerable<IKeyValueStore> keyStores;
    public KeyValueStoreFactory(IEnumerable<IKeyValueStore> keyStores)
        => this.keyStores = keyStores;


    IKeyValueStore? defaultStore;
    public IKeyValueStore DefaultStore => this.defaultStore ??= this.GetStore(DefaultStoreName);

    public void SetDefaultStore(string aliasName)
        => this.defaultStore = this.GetStore(aliasName);

    public string[] AvailableStores
        => this.keyStores.Select(x => x.Alias).ToArray();

    public bool HasStore(string aliasName)
        => this.keyStores.Any(x => x.Alias.Equals(aliasName, StringComparison.InvariantCultureIgnoreCase));

    public IKeyValueStore GetStore(string aliasName) =>
        this.keyStores.FirstOrDefault(x => x.Alias.Equals(aliasName, StringComparison.InvariantCultureIgnoreCase)) ??
        throw new ArgumentException("No key/value store named " + aliasName);
}

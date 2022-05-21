using System.ComponentModel;

namespace Shiny.Stores;


public interface IObjectStoreBinder
{
    /// <summary>
    /// Attempts to bind an object to a named store, if the alias is not passed, the binder will look at the attribute
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="keyValueStoreAlias"></param>
    void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null);


    /// <summary>
    /// Binds an object to a given store
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="store"></param>
    void Bind(INotifyPropertyChanged npc, IKeyValueStore store);


    /// <summary>
    /// Unbinds an object from whatever store it was bound to
    /// </summary>
    /// <param name="npc"></param>
    void UnBind(INotifyPropertyChanged npc);


    /// <summary>
    /// Unbinds any existing instances
    /// </summary>
    void UnBindAll();
}

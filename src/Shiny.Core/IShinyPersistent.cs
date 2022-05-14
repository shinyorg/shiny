namespace Shiny
{
    /// <summary>
    /// This is a marker interface to declare that you want Shiny to persist the public get/set NPC properties
    /// to a store.  NOTE: you must register with dependency injection
    /// </summary>
    public interface IShinyPersistent : System.ComponentModel.INotifyPropertyChanged
    {
    }
}


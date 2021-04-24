## State Restorable Services

This is pretty cool, imagine you want the state of your service preserved across restarts - Shiny does this in epic fashion

Simply turn your service implement INotifyPropertyChanged (or the easy Shiny.NotifyPropertyChanged) and register it in your shiny startup and Shiny will take care of the rest

```cs
using Shiny;

#region RestorableServices
public class MyBadAssService :
    NotifyPropertyChanged,
    IMyBadAssService,
    IShinyStartupTask
{
    int count;

    public int Count
    {
        get => this.count;
        set => this.Set(ref this.count, value);
    }

    public void Start()
    {
        this.Count++;
    }
}

#endregion

public interface IMyBadAssService
{
}
```

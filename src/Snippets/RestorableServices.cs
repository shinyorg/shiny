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
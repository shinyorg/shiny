using System.ComponentModel;

namespace Shiny.Tests.Core.Infrastructure;


public interface IFullService : INotifyPropertyChanged, IShinyStartupTask
{
    int Count { get; }
}


public class FullService : NotifyPropertyChanged, IFullService
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
        Console.WriteLine("Startup Count today is " + this.Count);
    }
}

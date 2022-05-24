using Shiny;

namespace Sample;


public class StartupTask : NotifyPropertyChanged, IShinyStartupTask
{
    int starts;
    public int NumberOfStarts
    {
        get => this.starts;
        set => this.Set(ref this.starts, value);
    }


    public void Start()
    {
        this.NumberOfStarts++;
        Console.WriteLine("Startup Task - Number of starts: " + this.NumberOfStarts);
    }
}


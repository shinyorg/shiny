using Shiny;

namespace Sample;

public class StartupTask : NotifyPropertyChanged, IShinyStartupTask
{
    public void Start()
    {
        Console.WriteLine("TEST");
    }
}


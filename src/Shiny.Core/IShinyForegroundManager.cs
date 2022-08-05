using System.ComponentModel;

namespace Shiny;


// TODO: only used by android - I need to get this out of here
public interface IShinyForegroundManager : INotifyPropertyChanged
{
    string? Title { get; set; }
    string? Message { get; set; }

    int Progress { get; set; }
    int Total { get; set; }
    bool IsIndeterministic { get; set; }
    //string? Channel { get; }
    //string? Ticker { get; }
}

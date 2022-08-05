using System;
using System.ComponentModel;


namespace Shiny
{
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
}

using System;
using System.ComponentModel;


namespace Shiny
{
    public interface IShinyForegroundManager : INotifyPropertyChanged
    {
        string? Title { get; }
        string? Message { get; }

        int Progress { get; }
        int Total { get; }
        bool IsIndeterministic { get; }
        //string? Channel { get; }
        //string? Ticker { get; }
    }
}

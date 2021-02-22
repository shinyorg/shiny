using System;
using System.ComponentModel;


namespace Shiny
{
    // if INPC, notification will change
    // always go to the default channel
    public interface IShinyForegroundManager : INotifyPropertyChanged
    {
        string? Title { get; }
        string? Message { get; }
        string? Channel { get; }
        int? Progress { get; }
        bool? IsIndeterministic { get; }
        //string? Ticker { get; }
    }
}

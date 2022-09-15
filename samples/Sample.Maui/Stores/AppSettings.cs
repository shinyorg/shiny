using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Shiny;

namespace Sample.Stores;


public interface IAppSettings : INotifyPropertyChanged
{
    bool IsChecked { get; set; }
    string YourText { get; set; }
    DateTime? LastUpdated { get; set; }
}


public class AppSettings : Shiny.NotifyPropertyChanged, IAppSettings
{
    public AppSettings()
    {
        this.WhenAnyProperty()
            .Skip(1)
            .Subscribe(_ => this.LastUpdated = DateTime.Now);
    }


    bool isChecked;
    public bool IsChecked
    {
        get => this.isChecked;
        set => this.Set(ref this.isChecked, value);
    }

    string yourText;
    public string YourText
    {
        get => this.yourText;
        set => this.Set(ref this.yourText, value);
    }


    DateTime? lastUpdated;
    public DateTime? LastUpdated
    {
        get => this.lastUpdated;
        set => this.Set(ref this.lastUpdated, value);
    }
}

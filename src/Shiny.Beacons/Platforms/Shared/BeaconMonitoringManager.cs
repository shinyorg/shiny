using System;


namespace Shiny.Beacons
{
    public partial class BeaconMonitoringManager : NotifyPropertyChanged, IShinyForegroundManager
    {
        string? title;
        public string? Title
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string? message;
        public string? Message
        {
            get => this.message;
            set => this.Set(ref this.message, value);
        }


        int progress;
        public int Progress
        {
            get => this.progress;
            set => this.Set(ref this.progress, value);
        }


        int total;
        public int Total
        {
            get => this.total;
            set => this.Set(ref this.total, value);
        }


        bool indeterministic;
        public bool IsIndeterministic
        {
            get => this.indeterministic;
            set => this.Set(ref this.indeterministic, value);
        }
    }
}

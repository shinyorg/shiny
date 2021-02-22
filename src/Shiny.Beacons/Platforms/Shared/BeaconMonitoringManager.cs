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


        int? progress;
        public int? Progress
        {
            get => this.progress;
            set => this.Set(ref this.progress, value);
        }


        bool? indeterministic;
        public bool? IsIndeterministic
        {
            get => this.indeterministic;
            set => this.Set(ref this.indeterministic, value);
        }


        string? ticker;
        public string? Ticker
        {
            get => this.ticker;
            set => this.Set(ref this.ticker, value);
        }


        string? channel;
        public string? Channel
        {
            get => this.channel;
            set => this.Set(ref this.channel, value);
        }
    }
}

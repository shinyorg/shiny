using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Shiny;
using Shiny.Locations;
using Xamarin.Forms;


namespace Sample
{
    public class ListViewModel : SampleViewModel
    {
        readonly IMotionActivityManager activityManager;
        CompositeDisposable? disposer;


        public ListViewModel()
        {
            this.activityManager = ShinyHost.Resolve<IMotionActivityManager>();

            this.Load = new Command(async () =>
            {
                var result = await this.activityManager.RequestAccess();
                if (result != AccessState.Available)
                {
                    await this.Alert("Motion Activity is not available - " + result);
                    return;
                }

                var activities = await this.activityManager.QueryByDate(this.Date);
                this.Events = activities
                    .OrderByDescending(x => x.Timestamp)
                    .Select(x => new CommandItem
                    {
                        Text = $"({x.Confidence}) {x.Types}",
                        Detail = $"{x.Timestamp.LocalDateTime}"
                    })
                    .ToList();

                this.EventCount = this.Events.Count;
            });
        }


        public override void OnAppearing()
        {
            base.OnAppearing();

            this.Load.Execute(null);
            this.disposer = new CompositeDisposable();

            this.activityManager
                .WhenActivityChanged()
                .SubOnMainThread(x => this.CurrentActivity = $"({x.Confidence}) {x.Types}")
                .DisposedBy(this.disposer);

            this.WhenAnyProperty(x => x.Date)
                .DistinctUntilChanged()
                .Subscribe(_ => this.Load.Execute(null))
                .DisposedBy(this.disposer);
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.disposer?.Dispose();
        }


        public ICommand Load { get; }


        DateTime date = DateTime.Now;
        public DateTime Date
        {
            get => this.date;
            set => this.Set(ref this.date, value);
        }


        int count;
        public int EventCount
        {
            get => this.count;
            private set => this.Set(ref this.count, value);
        }


        string activity;
        public string CurrentActivity
        {
            get => this.activity;
            private set => this.Set(ref this.activity, value);
        }


        IList<CommandItem> events;
        public IList<CommandItem> Events
        {
            get => this.events;
            private set
            {
                this.events = value;
                this.RaisePropertyChanged();
            }
        }
    }
}

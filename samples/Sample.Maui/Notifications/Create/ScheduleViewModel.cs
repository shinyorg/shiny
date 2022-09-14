using Shiny;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Xamarin.Forms;


namespace Sample.Create
{
    public class ScheduleViewModel : SampleViewModel
    {
        CompositeDisposable? disposer;


        public ScheduleViewModel()
        {
            this.SelectedDate = DateTime.Now;
            this.SelectedTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(3));

            this.Use = new Command(async () =>
            {
                if (this.ScheduledDateTime < DateTime.Now)
                {
                    await this.Alert("Scheduled Date & Time must be in the future");
                    return;
                }
                State.CurrentNotification!.ScheduleDate = this.ScheduledDateTime;
                State.CurrentNotification!.Geofence = null;
                State.CurrentNotification!.RepeatInterval = null;

                await this.Navigation.PopModalAsync();
            });

        }


        public override void OnAppearing()
        {
            this.disposer = new CompositeDisposable();

            this.WhenAnyProperty(x => x.SelectedDate)
                .Subscribe(_ => this.CalcDate())
                .DisposedBy(this.disposer);

            this.WhenAnyProperty(x => x.SelectedTime)
                .Subscribe(_ => this.CalcDate())
                .DisposedBy(this.disposer);
        }


        public ICommand Use { get; }


        DateTime date;
        public DateTime SelectedDate
        {
            get => this.date;
            set => this.Set(ref this.date, value);
        }


        TimeSpan time;
        public TimeSpan SelectedTime
        {
            get => this.time;
            set => this.Set(ref this.time, value);
        }


        DateTime scheduledDateTime;
        public DateTime ScheduledDateTime
        {
            get => this.scheduledDateTime;
            private set => this.Set(ref this.scheduledDateTime, value);
        }


        void CalcDate()
        {
            this.ScheduledDateTime = new DateTime(
                this.SelectedDate.Year,
                this.SelectedDate.Month,
                this.SelectedDate.Day,
                this.SelectedTime.Hours,
                this.SelectedTime.Minutes,
                0
            );
        }
    }
}

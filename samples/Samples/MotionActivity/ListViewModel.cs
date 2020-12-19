using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny.Locations;


namespace Samples.MotionActivity
{
    public class ListViewModel : ViewModel
    {
        readonly IMotionActivityManager? activityManager;


        public ListViewModel(IDialogs dialogs, IMotionActivityManager? activityManager = null)
        {
            this.activityManager = activityManager;

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                if (this.activityManager == null)
                {
                    await dialogs.Alert("MotionActivity is not supported on this platform");
                    return;
                }

                var result = await this.activityManager.RequestAccess();
                if (result != Shiny.AccessState.Available)
                {
                    await dialogs.Alert("Motion Activity is not available - " + result);
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
            this.BindBusyCommand(this.Load);

            this.WhenAnyValue(x => x.Date)
                .DistinctUntilChanged()
                .Select(_ => Unit.Default)
                .InvokeCommand((ICommand)this.Load)
                .DisposeWith(this.DestroyWith);
        }


        public override void OnAppearing()
        {
            base.OnAppearing();

            this.Load.Execute(null);
            this.activityManager?
                .WhenActivityChanged()
                .SubOnMainThread(x => this.CurrentActivity = $"({x.Confidence}) {x.Types}")
                .DisposeWith(this.DeactivateWith);
        }


        public ICommand Load { get; }
        [Reactive] public DateTime Date { get; set; } = DateTime.Now;
        [Reactive] public int EventCount { get; private set; }
        [Reactive] public string CurrentActivity { get; private set; }
        [Reactive] public IList<CommandItem> Events { get; private set; }
    }
}

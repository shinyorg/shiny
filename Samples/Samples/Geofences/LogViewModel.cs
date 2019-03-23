using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;


namespace Samples.Geofences
{
    public class LogViewModel : ViewModel
    {
        public LogViewModel(SampleSqliteConnection conn, IUserDialogs dialogs)
        {
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var events = await conn.GeofenceEvents.OrderBy(x => x.Date).ToListAsync();
                this.Events = events
                    .Select(x => new CommandItem
                    {
                        Text = x.Text,
                        Detail = x.Detail
                    })
                    .ToList();
                this.HasEvents = events.Any();
            });

            this.Clear = ReactiveCommand.CreateFromTask(async () =>
            {
                var confirm = await dialogs.ConfirmAsync(
                    "Do you wish to clear the geofence logs?",
                    "Confirm",
                    "Yes",
                    "No"
                );
                if (confirm)
                {
                    await conn.DeleteAllAsync<GeofenceEvent>();
                    this.Load.Execute(Unit.Default);
                }
            });
            this.BindBusyCommand(this.Load);
        }


        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }
        [Reactive] public bool HasEvents { get; private set; }
        [Reactive] public List<CommandItem> Events { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(Unit.Default);
        }
    }
}

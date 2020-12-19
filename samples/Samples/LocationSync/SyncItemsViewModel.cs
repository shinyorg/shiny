using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.LocationSync
{
    public class SyncItemsViewModel : ViewModel
    {
        public SyncItemsViewModel(SampleSqliteConnection conn)
        {
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var events = await conn.LocationSyncEvents
                    .OrderByDescending(x => x.DateCreated)
                    .ToListAsync();

                this.Events = events
                    .Select(x => new ItemViewModel(x))
                    .ToList();
            });
            this.BindBusyCommand(this.Load);
        }


        public ICommand Load { get; }
        [Reactive] public IList<ItemViewModel> Events { get; set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }
    }
}

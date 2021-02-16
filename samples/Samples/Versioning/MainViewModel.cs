using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Versioning;


namespace Samples.Versioning
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(SampleSqliteConnection conn, IVersionManager manager)
        {
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var list = await conn
                    .VersionChanges
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

                this.Logs = list
                    .Select(x => new CommandItem
                    {
                        Text = $"{x.Area} on {x.Date.ToLocalTime():MMM dd Y hh:mm tt}",
                        Detail = $"Old: {x.Old} - New: {x.New}"
                    })
                    .ToList();
            });

            this.BindBusyCommand(this.Load);
        }


        public override void OnAppearing() => this.Load.Execute(null);
        [Reactive] public List<CommandItem> Logs { get; private set; }
        public ICommand Load { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Infrastructure
{
    public abstract class AbstractLogViewModel : ViewModel
    {
        protected AbstractLogViewModel(IUserDialogs dialogs)
        {
            this.Dialogs = dialogs;

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var logs = await this.LoadLogs();
                this.Logs = logs.ToList();
                this.HasLogs = this.Logs.Any();
            });
            this.Clear = ReactiveCommand.CreateFromTask(this.DoClear);
            this.BindBusyCommand(this.Load);
        }


        protected IUserDialogs Dialogs { get; }
        [Reactive] public bool HasLogs { get; protected set; }
        [Reactive] public IList<CommandItem> Logs { get; protected set; }
        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            await this.Load.Execute();
        }


        protected abstract Task<IEnumerable<CommandItem>> LoadLogs();
        protected abstract Task ClearLogs();


        protected virtual async Task DoClear()
        {
            var confirm = await this.Dialogs.ConfirmAsync("Clear Logs?");
            if (confirm)
            {
                await this.ClearLogs();
                await this.Load.Execute();
            }
        }
    }
}

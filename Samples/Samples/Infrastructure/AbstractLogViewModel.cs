using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;


namespace Samples.Infrastructure
{
    public abstract class AbstractLogViewModel<TItem> : ViewModel
    {
        protected AbstractLogViewModel(IUserDialogs dialogs)
        {
            this.Dialogs = dialogs;

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                this.Logs.Clear();
                var logs = await this.LoadLogs();
                this.Logs.ReplaceAll(logs);
                this.HasLogs = this.Logs.Any();
            });
            this.Clear = ReactiveCommand.CreateFromTask(this.DoClear);
            this.BindBusyCommand(this.Load);
        }


        protected IUserDialogs Dialogs { get; }
        [Reactive] public bool HasLogs { get; protected set; }
        public ObservableList<TItem> Logs { get; } = new ObservableList<TItem>();
        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            await this.Load.Execute();
        }


        protected abstract Task<IEnumerable<TItem>> LoadLogs();
        protected abstract Task ClearLogs();


        protected virtual void InsertItem(TItem item)
            => this.Logs.Insert(0, item);

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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Shiny;

namespace Samples.Infrastructure
{
    public abstract class AbstractLogViewModel<TItem> : ViewModel
    {
        readonly object syncLock = new object();


        protected AbstractLogViewModel(IDialogs dialogs)
        {
            this.Dialogs = dialogs;

            this.Logs = new ObservableList<TItem>();
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var logs = await this.LoadLogs();
                this.Logs.ReplaceAll(logs);
            });
            this.Clear = ReactiveCommand.CreateFromTask(this.DoClear);
            this.BindBusyCommand(this.Load);

            this.WhenAnyValue(x => x.SelectedItem)
                .WhereNotNull()
                .SubscribeAsync(x => this.OnSelected(x))
                .DisposedBy(this.DestroyWith);
        }


        protected virtual Task OnSelected(TItem item) => Task.CompletedTask;

        protected IDialogs Dialogs { get; }
        public ObservableList<TItem> Logs { get; }
        [Reactive] public TItem SelectedItem { get; set; }
        public ICommand Load { get; }
        public ICommand Clear { get; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }


        protected abstract Task<IEnumerable<TItem>> LoadLogs();
        protected abstract Task ClearLogs();
        protected virtual void InsertItem(TItem item)
        {
            lock (this.syncLock)
                this.Logs.Insert(0, item);
        }


        protected virtual async Task DoClear()
        {
            var confirm = await this.Dialogs.Confirm("Clear Logs?");
            if (confirm)
            {
                await this.ClearLogs();
                this.Load.Execute(null);
            }
        }
    }
}

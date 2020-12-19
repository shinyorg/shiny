using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
            this.Logs
                .WhenCollectionChanged()
                .Synchronize(this.syncLock)
                .Select(_ => this.Logs.Any())
                .Subscribe(x => this.HasLogs = x)
                .DisposedBy(this.DestroyWith);

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var logs = await this.LoadLogs();
                this.Logs.ReplaceAll(logs);
            });
            this.Clear = ReactiveCommand.CreateFromTask(this.DoClear);
            this.BindBusyCommand(this.Load);
        }


        protected IDialogs Dialogs { get; }
        public ObservableList<TItem> Logs { get; }
        public ICommand Load { get; }
        public ICommand Clear { get; }
        [Reactive] public bool HasLogs { get; private set; }


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

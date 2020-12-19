using System;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.BluetoothLE
{
    public class TaskViewModel : ViewModel
    {
        CancellationTokenSource? cancelSrc;


        public TaskViewModel(string text, Func<CancellationToken, Task> run, IObservable<bool>? whenAny = null)
        {
            this.Text = text;

            this.Start = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    this.cancelSrc = new CancellationTokenSource();
                    await run(this.cancelSrc.Token);
                },
                whenAny
            );
            this.BindBusyCommand(this.Start);

            this.Stop = ReactiveCommand.Create(() =>
            {
                this.IsBusy = false;
                this.cancelSrc?.Cancel();
                this.cancelSrc = null;
            });
        }


        public IReactiveCommand Start { get; }
        public IReactiveCommand Stop { get; }
        public string Text { get; }
        public string? ButtonText { [ObservableAsProperty] get; }
    }
}


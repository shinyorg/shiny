using System;
using ReactiveUI;
using Samples.Infrastructure;
using Shiny;
using Shiny.Logging;


namespace Samples
{
    public class GlobalExceptionHandler : IObserver<Exception>, IShinyStartupTask
    {
        readonly IDialogs dialogs;
        public GlobalExceptionHandler(IDialogs dialogs) => this.dialogs = dialogs;


        public void Start() => RxApp.DefaultExceptionHandler = this;
        public void OnCompleted() {}
        public void OnError(Exception error) {}


        public async void OnNext(Exception value)
        {
            Log.Write(value);
            await this.dialogs.Alert(value.ToString(), "ERROR");
        }
    }
}
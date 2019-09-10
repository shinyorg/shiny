using System;
using System.Reactive.Disposables;
using ReactiveUI;
using Shiny.Net;


namespace Shiny.Integrations.ReactiveUI
{
    public class ShinyReactiveObject : ReactiveObject, IActivatableViewModel
    {
        CompositeDisposable compositeCapture;


        protected ShinyReactiveObject()
        {
            this.WhenActivated(disposables =>
            {
                this.compositeCapture = disposables;

                ShinyHost
                    .Resolve<IConnectivity>()
                    .WhenInternetStatusChanged()
                    .SubOnMainThread(x => this.IsInternetAvailable = x)
                    .DisposeWith(disposables);
            });
        }


        bool busy;
        public bool IsBusy
        {
            get => this.busy;
            protected set => this.RaiseAndSetIfChanged(ref this.busy, value);
        }


        bool isInternetAvailable;
        public bool IsInternetAvailable
        {
            get => this.isInternetAvailable;
            private set => this.RaiseAndSetIfChanged(ref this.isInternetAvailable, value);
        }


        protected void BindBusyCommand(IReactiveCommand command) =>
            command.IsExecuting.Subscribe(
                x => this.IsBusy = x,
                _ => this.IsBusy = false,
                () => this.IsBusy = false
            )
            .DisposeWith(this.compositeCapture);


        public ViewModelActivator Activator { get; set; } = new ViewModelActivator();
    }
}

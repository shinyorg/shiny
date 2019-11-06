using System;
using System.Reactive.Disposables;
using MvvmCross.ViewModels;
using Shiny.Net;

namespace Shiny.Integrations.MvvmCross
{
    public class ShinyMvxViewModel : MvxViewModel
    {
        bool isInternetAvailable;
        public bool IsInternetAvailable
        {
            get => this.isInternetAvailable;
            private set => this.RaiseAndSetIfChanged(ref this.isInternetAvailable, value);
        }


        public override void ViewAppeared()
        {
            base.ViewAppeared();
            ShinyHost
                .Resolve<IConnectivity>()
                .WhenInternetStatusChanged()
                .Subscribe(x => this.IsInternetAvailable = x)
                .DisposedBy(this.DeactivateWith);
        }


        public override void ViewDisappeared()
        {
            base.ViewDisappeared();
            this.deactivateWith?.Dispose();
            this.deactivateWith = null;
        }


        public override void ViewDestroy(bool viewFinishing = true)
        {
            base.ViewDestroy(viewFinishing);
            this.DestroyWith?.Dispose();
        }


        CompositeDisposable deactivateWith;
        protected CompositeDisposable DeactivateWith
        {
            get
            {
                if (this.deactivateWith == null)
                    this.deactivateWith = new CompositeDisposable();

                return this.deactivateWith;
            }
        }


        protected CompositeDisposable DestroyWith { get; } = new CompositeDisposable();
    }
}

using System;
using System.Reactive.Disposables;
using Prism;
using Prism.AppModel;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Services;
using Shiny.Net;
using Xamarin.Forms;

namespace Shiny.Integrations.Prism
{
    public class ShinyBindableBase : BindableBase, IPageLifecycleAware, IDestructible
    {
        bool inetAvail;
        public bool IsInternetAvailable
        {
            get => this.inetAvail;
            private set => this.SetProperty(ref this.inetAvail, value);
        }


        public virtual void OnAppearing()
        {
            var device = (IDeviceService)PrismApplicationBase.Current.Container.Resolve(typeof(IDeviceService));
            ShinyHost
                .Resolve<IConnectivity>()
                .WhenInternetStatusChanged()
                .Subscribe(x => device.BeginInvokeOnMainThread(() =>
                    this.IsInternetAvailable = x
                ))
                .DisposedBy(this.DeactivateWith);
        }


        public virtual void OnDisappearing()
        {
            this.deactivateWith?.Dispose();
            this.deactivateWith = null;
        }


        public virtual void Destroy()
        {
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

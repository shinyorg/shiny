using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Prism.AppModel;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples
{
    public abstract class ViewModel : ReactiveObject,
                                      INavigatingAware,
                                      INavigatedAware,
                                      IPageLifecycleAware,
                                      IDestructible,
                                      IConfirmNavigationAsync
    {
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


        public virtual void OnAppearing()
        {
        }


        public virtual void OnDisappearing()
        {
            this.deactivateWith?.Dispose();
            this.deactivateWith = null;
        }


        public virtual void OnNavigatingTo(INavigationParameters parameters)
        {
            if (parameters.GetNavigationMode() == NavigationMode.New)
                this.OnStart();
        }


        protected virtual void OnStart()
        {
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
        }


        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
        }


        public virtual void Destroy()
        {
            this.DestroyWith?.Dispose();
        }


        public virtual Task<bool> CanNavigateAsync(INavigationParameters parameters) => Task.FromResult(true);
        [Reactive] public bool IsBusy { get; protected set; }
        [Reactive] public string Title { get; protected set; }

        protected void BindBusyCommand<T, U>(ReactiveCommand<T, U> reactiveCommand)
            => reactiveCommand
                .IsExecuting
                .Subscribe(x => this.IsBusy = x)
                .DisposeWith(this.DeactivateWith);
    }
}

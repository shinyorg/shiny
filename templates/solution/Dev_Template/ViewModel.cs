using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Prism.AppModel;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace $safeprojectname$
{
    public abstract class ViewModel : ReactiveObject,
                                      IAutoInitialize,
                                      IInitialize,
                                      IInitializeAsync,
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


        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
            this.deactivateWith?.Dispose();
            this.deactivateWith = null;
        }

        public virtual void Initialize(INavigationParameters parameters) { }
        public virtual Task InitializeAsync(INavigationParameters parameters) => Task.CompletedTask;
        public virtual void OnNavigatedTo(INavigationParameters parameters) {}
        public virtual void OnAppearing() {}
        public virtual void OnDisappearing() {}
        public virtual void Destroy() => this.DestroyWith?.Dispose();
        public virtual Task<bool> CanNavigateAsync(INavigationParameters parameters) => Task.FromResult(true);

        [Reactive] public bool IsBusy { get; protected set; }
        [Reactive] public string Title { get; protected set; }


        protected void BindBusyCommand(IReactiveCommand command) =>
            command.IsExecuting.Subscribe(
                x => this.IsBusy = x,
                _ => this.IsBusy = false,
                () => this.IsBusy = false
            )
            .DisposeWith(this.DestroyWith);
    }
}

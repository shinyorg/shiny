using System.Reactive.Disposables;

namespace Sample.Infrastructure;


public abstract class ViewModel : ReactiveObject,
                                  IInitializeAsync,
                                  IConfirmNavigationAsync,
                                  INavigationAware,
                                  IPageLifecycleAware,
                                  IDisposable
{
    protected ViewModel(BaseServices services)
    {
        this.Services = services;
        this.Navigate = ReactiveCommand.CreateFromTask<string>(uri =>
            this.Navigation.Navigate(uri)
        );
    }


    [Reactive] public string Title { get; protected set; } = null!;
    [Reactive] public bool IsBusy { get; protected set; }
    
    protected IPlatform Platform => this.Services.Platform;
    protected IPageDialogService Dialogs => this.Services.Dialogs;
    protected INavigationService Navigation => this.Services.Navigator;
    protected BaseServices Services { get; }

    public ICommand Navigate { get; }

    public virtual void OnAppearing() { }
    public virtual void OnDisappearing()
    {
        this.deactivateWith?.Dispose();
        this.deactivateWith = null;
    }


    ILogger? logger;
    protected ILogger Logger
    {
        get
        {
            this.logger ??= this.Services.LoggerFactory.CreateLogger(this.GetType());
            return this.logger;
        }
    }

    CancellationTokenSource? cancelSrc;
    public CancellationToken CancelToken
    {
        get
        {
            this.cancelSrc ??= new();
            return this.cancelSrc.Token;
        }
    }

    CompositeDisposable? deactivateWith;
    public CompositeDisposable DeactivateWith
    {
        get => this.deactivateWith ??= new();
    }

    CompositeDisposable? destroyWith;
    public CompositeDisposable DestroyWith
    {
        get => this.destroyWith ??= new();
    }

    public virtual Task InitializeAsync(INavigationParameters parameters) => Task.CompletedTask;
    public virtual Task<bool> CanNavigateAsync(INavigationParameters parameters) => Task.FromResult(true);
    public virtual void OnNavigatedFrom(INavigationParameters parameters) { }
    public virtual void OnNavigatedTo(INavigationParameters parameters) { }
    public virtual void Dispose()
    {
        this.cancelSrc?.Cancel();
        this.cancelSrc = null;
        this.destroyWith?.Dispose();
        this.destroyWith = null;
    }


    protected virtual ICommand LoadingCommand(Action action, IObservable<bool>? canExecute = null)
    {
        var cmd = ReactiveCommand.Create(action, canExecute);
        cmd.Subscribe(
            x => this.IsBusy = true,
            _ => this.IsBusy = false,
            () => this.IsBusy = false
        );
        return cmd;
    }


    protected virtual ICommand LoadingCommand(Func<Task> task, IObservable<bool>? canExecute = null)
    {
        var cmd = ReactiveCommand.CreateFromTask(task, canExecute);
        cmd.Subscribe(
            x => this.IsBusy = true,
            _ => this.IsBusy = false,
            () => this.IsBusy = false
        );
        return cmd;
    }


    protected virtual ICommand ConfirmCommand(string question, Func<Task> func, IObservable<bool>? canExecute = null) => ReactiveCommand.CreateFromTask(async () =>
    {
        var result = await this.Dialogs.DisplayAlertAsync("", question, "Yes", "No");
        if (result)
            await func.Invoke();
    }, canExecute);


    protected virtual Task Alert(string message, string title = "ERROR", string okBtn = "OK")
        => this.Dialogs.DisplayAlertAsync(title, message, okBtn);

    protected virtual Task<bool> Confirm(string question, string title = "Confirm")
        => this.Dialogs.DisplayAlertAsync(title, question, "Yes", "No");

    protected virtual async Task SafeExecute(Func<Task> task)
    {
        try
        {
            await task.Invoke();
        }
        catch (Exception ex)
        {
            await this.DisplayError(ex);
        }
    }


    protected virtual async Task DisplayError(Exception ex)
    {
        this.Logger.LogError(ex, "Error");
        await this.Dialogs.DisplayAlertAsync("Error", ex.ToString(), "OK");
    }


    public bool IsApple => this.IsIos || this.IsMacCatalyst;
#if IOS
    public bool IsIos => true;
    public bool IsMacCatalyst => false;
    public bool IsAndroid => false;
#elif MACCATALYST
    public bool IsIos => false;
    public bool IsMacCatalyst => true;
    public bool IsAndroid => false;
#elif ANDROID
    public bool IsIos => false;
    public bool IsMacCatalyst => false;
    public bool IsAndroid => true;
#endif
}


public record BaseServices(
    IPlatform Platform,
    ILoggerFactory LoggerFactory,
    IPageDialogService Dialogs,
    INavigationService Navigator
);
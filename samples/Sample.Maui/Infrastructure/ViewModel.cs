using System.Reactive.Disposables;

namespace Sample.Infrastructure;


public abstract class ViewModel : ReactiveObject, IInitializeAsync, IConfirmNavigationAsync, INavigationAware, IDisposable
{
    readonly BaseServices services;
    protected ViewModel(BaseServices services) => this.services = services;


    [Reactive] public bool IsBusy { get; protected set; }
    protected IPlatform Platform => this.services.Platform;
    protected IPageDialogService Dialogs => this.services.Dialogs;
    protected INavigationService Navigation => this.services.Navigator;


    ILogger? logger;
    protected ILogger Logger
    {
        get
        {
            this.logger ??= this.services.LoggerFactory.CreateLogger(this.GetType());
            return this.logger;
        }
    }


    public CompositeDisposable DestroyWith { get; } = new();
    public virtual Task InitializeAsync(INavigationParameters parameters) => Task.CompletedTask;
    public virtual Task<bool> CanNavigateAsync(INavigationParameters parameters) => Task.FromResult(true);
    public virtual void OnNavigatedFrom(INavigationParameters parameters) { }
    public virtual void OnNavigatedTo(INavigationParameters parameters) { }
    public virtual void Dispose() => this.DestroyWith.Dispose();


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
}


public record BaseServices(
    IPlatform Platform,
    ILoggerFactory LoggerFactory,
    IPageDialogService Dialogs,
    INavigationService Navigator
);
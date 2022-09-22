using Prism.Common;
using Prism.Navigation.Xaml;

namespace Sample;


public class CommandExceptionHandler : IObserver<Exception>, IShinyStartupTask
{
    readonly IApplication application;
    readonly IPlatform platform;
    readonly ILogger logger;


    public CommandExceptionHandler(
        IApplication application,
        IPlatform platform,
        ILogger<CommandExceptionHandler> logger
    )
    {
        this.application = application;
        this.platform = platform;
        this.logger = logger;
    }


    public void Start() => RxApp.DefaultExceptionHandler = this;
    public void OnCompleted() { }
    public void OnError(Exception error) { }
    public async void OnNext(Exception value)
    {
        this.logger.LogError(value, "Command exception caught");
        await this.Run(x => x.DisplayAlertAsync("ERROR", value.ToString(), "OK"));
    }


    async Task Run(Func<IPageDialogService, Task> func)
    {
        var window = this.application.Windows.OfType<Window>().First();
        var currentPage = MvvmHelpers.GetCurrentPage(window.Page);
        var container = currentPage.GetContainerProvider();

        var dialogs = container.Resolve<IPageDialogService>();
        await this.platform.InvokeTaskOnMainThread(() => func(dialogs));
    }
}
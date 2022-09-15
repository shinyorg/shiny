namespace Sample;

public class CommandExceptionHandler : IObserver<Exception>, IShinyStartupTask
{
    readonly ILogger logger;


    public CommandExceptionHandler(ILogger<CommandExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public void Start() => RxApp.DefaultExceptionHandler = this;
    public void OnCompleted() { }
    public void OnError(Exception error) { }
    public void OnNext(Exception value)
    {
        this.logger.LogError(value, "Command exception caught");
        // TODO: dialogs
    }
    
}


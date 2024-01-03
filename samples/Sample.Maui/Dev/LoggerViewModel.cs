namespace Sample.Dev;


public class LoggerViewModel : ViewModel
{
    public LoggerViewModel(BaseServices services) : base(services)
    {
        this.LogIt = ReactiveCommand.Create<string>(e =>
        {
            var l = Enum.Parse<LogLevel>(e);
            using var _ = this.Logger.BeginScope(new Dictionary<string, string>
            {
                { "BeginScopeVar", "BeginScopeValue" }
            });
            var ex = new Exception(e.ToUpper());

            switch (l)
            {
                case LogLevel.Trace:
                    this.Logger.LogTrace("TEST");
                    break;

                case LogLevel.Debug:
                    this.Logger.LogDebug("TEST");
                    break;

                case LogLevel.Information:
                    this.Logger.LogInformation("TEST");
                    break;

                case LogLevel.Warning:
                    this.Logger.LogWarning(ex, "TEST");
                    break;

                case LogLevel.Error:
                    this.Logger.LogError(ex, "TEST");
                    break;

                case LogLevel.Critical:
                    this.Logger.LogCritical(ex, "TEST");
                    break;
            }
        });
    }

    public ICommand LogIt { get; }
}
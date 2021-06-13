Title: Global Command Exception Handler
Order: 3
---


```csharp
public class SampleStartup : FrameworkStartup
{
    protected override void Configure(ILoggingBuilder builder, IServiceCollection services)
    {
        services.UseGlobalCommandExceptionHandler(x => {
            x.AlertType = ErrorAlertType.FullError;
            x.LocalizeErrorBodyKey = "";
            x.LocalizeErrorTitleKey = "";
            x.LogError = true;
        });
    }
}
```
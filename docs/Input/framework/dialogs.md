Title: Dialogs
Order: 4
---

```csharp
public class SampleStartup : FrameworkStartup
{
    protected override void Configure(ILoggingBuilder builder, IServiceCollection services)
    {
        // this will be automatically registered unless you register your own implementation IDialogs
        services.UseXfMaterialDialogs();
    }
}
```
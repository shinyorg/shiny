Title: Microsoft Authentication Library (MSAL)
Order: 99
---

Install <?# NugetShield "Shiny.Msal" /?> 

```csharp
public class SampleStartup : FrameworkStartup
{
    protected override void Configure(ILoggingBuilder builder, IServiceCollection services)
    {
        services.UseMsal(new MsalConfiguration(
            Secrets.Values.MsalClientId,
            Secrets.Values.MsalScopes.Split(';')
        )
        {
        //    //TenantId = Secrets.Values.MsalTenantId,
        //    //UseBroker = true,
        //    //SignatureHash = Secrets.Values.MsalSignatureHash
        });
    }
}
```
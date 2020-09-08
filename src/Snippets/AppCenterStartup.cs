using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class AppCenterStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseAppCenterLogging("android=YourAppSecret;ios=YourAppSecret");
    }
}
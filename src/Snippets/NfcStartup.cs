using Microsoft.Extensions.DependencyInjection;
using Shiny;


public class NfcStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services, IPlatform platform)
    {
        services.UseNfc();
    }
}
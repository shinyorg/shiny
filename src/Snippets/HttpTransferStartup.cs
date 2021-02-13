using Microsoft.Extensions.DependencyInjection;
using Shiny;


public class HttpTransferStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services, IPlatform platform)
    {
        services.UseHttpTransfers<HttpTransferDelegate>();
    }
}
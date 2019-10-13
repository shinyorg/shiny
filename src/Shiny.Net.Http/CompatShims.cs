using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;

namespace Shiny.Net.Http
{
    public class NetHttpServiceModuleAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
           // services.UseHttpTransfers();
        }
    }


    public static class CrossHttpTransfers
    {
        public static IHttpTransferManager Current => ShinyHost.Resolve<IHttpTransferManager>();
    }
}

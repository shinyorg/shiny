using System;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;


namespace Shiny
{
    public class FormsShinyApp : Application, IShinyStartup
    {
        public virtual void ConfigureApp(IServiceProvider provider)
        {
        }


        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.UseXamarinFormsDependencyService();
        }


        public virtual IServiceProvider CreateServiceProvider(IServiceCollection services) => null;
    }
}

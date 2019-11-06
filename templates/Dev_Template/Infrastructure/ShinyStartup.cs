using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Acr.UserDialogs.Forms;
using $safeprojectname$.Localization.Resx;
using $safeprojectname$.Data;
using $safeprojectname$.AppCenterIntegration;


namespace $safeprojectname$.Infrastructure
{
    public class ShinyStartup : Startup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.RegisterModule(new AppCenterModule());
            services.RegisterModule(new DataModule());

            services.AddSingleton<IUserDialogs, UserDialogs>();
            services.AddSingleton<ILocalize, ResxLocalize>();
            services.AddSingleton<ICoreServices, CoreServicesImpl>();
            services.RegisterStartupTask<GlobalExceptionHandler>();
        }
    }
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Prism;
using Acr.UserDialogs.Forms;
using $safeprojectname$.Infrastructure;
using $safeprojectname$.Data;


namespace $safeprojectname$
{
    public class MyShinyStartup : PrismStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.UseAppCenterLogging(Constants.AppCenterTokens, true, false);
            services.RegisterModule(new DataModule());

            services.AddSingleton<IUserDialogs, UserDialogs>();
            services.AddSingleton<ILocalize, ResxLocalize>();
            services.AddSingleton<CoreServices>();
            services.RegisterStartupTask<GlobalExceptionHandler>();

            services.UseNotifications(true);
            // services.UsePush

        }
    }
}
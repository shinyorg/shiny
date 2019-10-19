using System;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Logging;


namespace $safeprojectname$
{
    public class AppCenterModule : Module
    {
        public override void Register(IServiceCollection services)
        {
            AppCenter.Start
            (
                Constants.AppCenterToken,
                typeof(Analytics),
                typeof(Crashes)
                //typeof(Distribute)
            );

#if DEBUG
            Log.UseConsole();
            Log.UseDebug();
#endif
            var logger = new AppCenterLogger();
            Log.AddLogger(logger, true, true);
            services.AddSingleton<ILogger>(logger);
        }
    }
}

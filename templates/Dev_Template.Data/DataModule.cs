using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Jobs;
using Shiny.Net.Http;


namespace $safeprojectname$
{
    public class DataModule : Module
    {
        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<SqliteConnection>();
            services.UseHttpTransfers<HttpTransferDelegate>();
            services.RegisterJob(new JobInfo
            {
                Identifier = nameof(SyncJob),
                Type = typeof(SyncJob),
                BatteryNotLow = true,
                RequiredInternetAccess = InternetAccess.Any,
                Repeat = true
            });
        }
    }
}

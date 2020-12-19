using System;
using Microsoft.Extensions.DependencyInjection;



namespace Samples.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResumingFileResult();
            //services
            //    .AddAuthentication("BasicAuthentication")
            //    .AddScheme<AuthenticationSchemeOptions, BasiAuthenticationHandler>("BasicAuthentication", null);
        }


        //public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        //{
        //    if (env.IsDevelopment())
        //        app.UseDeveloperExceptionPage();

        //}
    }
}

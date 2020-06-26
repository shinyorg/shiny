using System;
using Android.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Shiny.Hosting
{
    public static partial class Extensions
    {
        public static void UseShinyAndroid(this IHostBuilder builder)
        {
            var app = Application.Context.ApplicationContext as ShinyApplication;
            if (app == null)
                throw new ArgumentException("You must have a main Android Application class that inherits from Shiny.Hosting.ShinyApplication");

            
            // TODO: dev?
            //builder.UseEnvironment("Android");
            // IHostLifetime instead of IHostApplicationLifetime on Application?

            //_hostingEnvironment = new HostingEnvironment()
            //{
            //    ApplicationName = _hostConfiguration[HostDefaults.ApplicationKey],
            //    EnvironmentName = _hostConfiguration[HostDefaults.EnvironmentKey] ?? Environments.Production,
            //    ContentRootPath = ResolveContentRootPath(_hostConfiguration[HostDefaults.ContentRootKey], AppContext.BaseDirectory),
            //};

            //if (string.IsNullOrEmpty(_hostingEnvironment.ApplicationName))
            //{
            //    // Note GetEntryAssembly returns null for the net4x console test runner.
            //    _hostingEnvironment.ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name;
            //}

            //_hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(_hostingEnvironment.ContentRootPath);
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime>(app);

            });
        }
    }
}

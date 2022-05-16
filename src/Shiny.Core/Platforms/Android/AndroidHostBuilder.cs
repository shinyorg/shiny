using System;
using Android.App;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public class AndroidHostBuilder : HostBuilder
{

    // TODO: if not MAUI, this guy should setup AndroidLifecycle
    //new AndroidLifecycle(androidApp);

    // TODO: register host as IAndroidHost & IHost
    protected override void PreBuildPlatformHost()
    {

    }

    protected override IHost BuildPlatformHost(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {

        return new AndroidHost(
            (Application)Application.Context,
            serviceProvider,
            loggerFactory
        );
    }
}

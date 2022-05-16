using Android.App;

namespace Shiny.Hosting;


public class AndroidHostBuilder : HostBuilder
{
    public AndroidHostBuilder(Application androidApp)
    {
        // TODO: if not MAUI, this guy should setup AndroidLifecycle
        //new AndroidLifecycle(androidApp);

        // TODO: register host as IAndroidHost & IHost
    }
}

-------------------------
Shiny.Integrations.SQLite
-------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


This integration package contains plugins for
* Error/Event Logging
* Repository (underlying stores)
* Settings
* Caching

SETUP
-----

To use any of these modules, in your shiny startup

using Shiny;

namespace Samples.ShinySetup
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // mix and match however you see fit
            services.UseSqliteLogging(true, false);
            services.UseSqliteCache();
            services.UseSqliteSettings();
            services.UseSqliteStorage();
        }
    }
}


WARNING
-------
It is not recommended that you switch the storage engine (Repository) on Shiny on existing apps.  There is no migration mechanism for this in Shiny (nor will there be)
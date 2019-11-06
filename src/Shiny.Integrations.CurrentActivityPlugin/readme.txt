---------------------------------------------------
Shiny.Integrations.CurrentActivityPlugin
---------------------------------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


Replaces the internal Shiny current activity monitor with James Montemagno 's current activity plugin https://github.com/JamesMontemagno/CurrentActivityPlugin
This library is only installed into your Android head project.


In your Android application file:

public class MainApplication : Application
{
    public MainApplication() : base() { }
    public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
    {
    }


    public override void OnCreate()
    {
        base.OnCreate();
        AndroidShinyHost.Init(
            this,
            new SampleStartup(),
            services =>
            {
                services.UseCurrentActivityPlugin();
            }
        );
    }
}
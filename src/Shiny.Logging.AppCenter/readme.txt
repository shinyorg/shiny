-----------------------
Shiny.Logging.AppCenter
-----------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

-----------------------
Setup
-----------------------

public class SampleStartup : Startup
{
    public override void ConfigureServices(IServiceCollection builder)
    {
        builder.UseAppCenterLogging(Constants.AppCenterTokens, true, false);
    }
}
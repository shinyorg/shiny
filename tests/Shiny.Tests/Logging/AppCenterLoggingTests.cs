#if IOS || ANDROID
using Microsoft.Extensions.Logging;

namespace Shiny.Tests.Logging;


public class AppCenterLoggingTests : AbstractShinyTests
{
    public AppCenterLoggingTests(ITestOutputHelper output) : base(output) {}


    protected override void Configure(HostBuilder hostBuilder)
    {
        //hostBuilder.Logging.AddAppCenter("ios=7d746922-5e14-45c6-bfb5-9e86d79930b9;android=;");
        base.Configure(hostBuilder);
    }


    [Fact(DisplayName = "Logging - AppCenter - End-to-End")]
    public void EndToEnd()
    {
        var logger = this.GetService<ILogger<AppCenterLoggingTests>>();
        logger.LogTrace("Info Test");
        logger.LogDebug("Debug Test");
        logger.LogWarning("Warning Test");
        logger.LogInformation("Info Test");
        logger.LogError(new InvalidProgramException("Error Exception"), "Error Test");
        logger.LogCritical(new InvalidProgramException("Critical Exception"), "Critical Test");

        this.Log("You will need to restart the app before appcenter ships logs and ensure the debugger is not attached");
    }
}

#endif
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


public class AndroidSummaryTransferNotification : IShinyStartupTask
{
    readonly AndroidPlatform platform;
    readonly IHttpTransferManager manager;
    readonly ILogger logger;
    

    public AndroidSummaryTransferNotification(
        AndroidPlatform platform,
        IHttpTransferManager manager,
        ILogger<AndroidSummaryTransferNotification> logger
    )
    {
        this.platform = platform;
        this.manager = manager;
        this.logger = logger;
    }


    public void Start()
    {
    }
}
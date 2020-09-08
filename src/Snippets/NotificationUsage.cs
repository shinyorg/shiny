using System.Threading.Tasks;
using Shiny;
using Shiny.Notifications;

public class NotificationUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<INotificationManager>();
    }
}

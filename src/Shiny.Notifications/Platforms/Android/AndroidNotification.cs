using System;
using System.Threading.Tasks;
using AndroidX.Core.App;

namespace Shiny.Notifications;


public class AndroidNotification : Notification
{
    public Func<Channel, NotificationCompat.Builder, Task>? Customize { get; }
}
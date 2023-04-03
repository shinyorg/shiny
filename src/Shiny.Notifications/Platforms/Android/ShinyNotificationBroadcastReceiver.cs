using System.Threading.Tasks;
using Android.Content;

namespace Shiny.Notifications;


[BroadcastReceiver(
    Name = ReceiverName,
    Enabled = true,
    Exported = false
)]
public class ShinyNotificationBroadcastReceiver : ShinyBroadcastReceiver
{
    public const string ReceiverName = "com.shiny.notifications." + nameof(ShinyNotificationBroadcastReceiver);
    public const string EntryIntentAction = ReceiverName + ".ENTRY_ACTION";
    public const string AlarmIntentAction = ReceiverName + ".ALARM_ACTION";


    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        switch (intent?.Action)
        {
            case Intent.ActionBootCompleted:
                this.Resolve<AndroidNotificationProcessor>().ProcessPending();
                break;

            case AlarmIntentAction:
                this.Resolve<AndroidNotificationProcessor>().ProcessAlarm(intent);
                break;

            case EntryIntentAction:
                await this.Resolve<AndroidNotificationProcessor>().TryProcessIntent(intent);
                //context?.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));
                break;
        }
    }
}

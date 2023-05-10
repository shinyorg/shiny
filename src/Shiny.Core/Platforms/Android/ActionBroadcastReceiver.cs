using System;
using Android.Content;

namespace Shiny;

[BroadcastReceiver(Enabled = true, Exported = false, Label = "Shiny Broadcast Receiver")]
public class ActionBroadcastReceiver : BroadcastReceiver
{
    readonly Action<Intent> action;
    public ActionBroadcastReceiver(Action<Intent> action) => this.action = action;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent != null)
            this.action.Invoke(intent);   
    }


    public static ActionBroadcastReceiver Register(AndroidPlatform platform, string intentAction, Action<Intent> action)
    {
         var filter = new IntentFilter();
         filter.AddAction(intentAction);
         var receiver = new ActionBroadcastReceiver(action);
         platform.AppContext.RegisterReceiver(receiver, filter);

         return receiver;
    }


    public static void UnRegister(AndroidPlatform platform, ActionBroadcastReceiver receiver)
    {
        platform.AppContext.UnregisterReceiver(receiver);
    }
}
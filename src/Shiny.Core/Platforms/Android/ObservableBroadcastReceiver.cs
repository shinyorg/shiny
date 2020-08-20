using System;
using Android.Content;


namespace Shiny
{
    public class ObservableBroadcastReceiver : BroadcastReceiver
    {
        public Action<Intent>? OnEvent { get; set; }
        public override void OnReceive(Context context, Intent intent) => this.OnEvent?.Invoke(intent);
    }
}

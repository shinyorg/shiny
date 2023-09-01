using Android.App;

namespace Shiny.Notifications;


public class AndroidChannel : Channel
{
#if !MONOANDROID
    public bool? Blockable { get; set; }
#endif
    public bool? AllowBubbles { get; set; }
    public bool? ShowBadge { get; set; }
    public bool? EnableLights { get; set; }
    public bool? EnableVibration { get; set; }
    public bool? BypassDnd { get; set; }
    public NotificationVisibility? LockscreenVisibility { get; set; }
}


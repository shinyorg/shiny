using System;
using Com.OneSignal.Abstractions;


namespace Shiny.Push.OneSignal
{
    public class OneSignalPushConfig
    {
        public OneSignalPushConfig(string appId) => this.AppId = appId;

        public string AppId { get; }
        public bool iOSAutoPrompt { get; set; }
        public bool iOSInAppLaunchURL { get; set; }
        public LOG_LEVEL LogLevel { get; set; } = LOG_LEVEL.ERROR;
        public LOG_LEVEL VisualLogLevel { get; set; } = LOG_LEVEL.FATAL;
        public OSInFocusDisplayOption InFocusDisplay { get; set; } = OSInFocusDisplayOption.Notification;
    }
}

using System;


namespace Shiny.Notifications
{
    public class UwpOptions
    {
        public static bool DefaultUseLongDuration { get; set; }
        public bool UseLongDuration { get; set; } = DefaultUseLongDuration;
    }
}

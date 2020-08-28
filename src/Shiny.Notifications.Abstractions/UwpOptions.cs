using System;


namespace Shiny.Notifications
{
    public class UwpOptions
    {
        public static bool DefaultUseLongDuration { get; set; }
        public static string DefaultGroupName { get; set; } = "Shiny";

        public bool UseLongDuration { get; set; } = DefaultUseLongDuration;
        public string GroupName { get; set; } = DefaultGroupName;
    }
}

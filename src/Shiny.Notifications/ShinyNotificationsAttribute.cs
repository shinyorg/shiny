using System;


namespace Shiny.Notifications
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class ShinyNotificationsAttribute : Attribute
    {
        public ShinyNotificationsAttribute(bool requestPermissionOnStart) { }
    }
}

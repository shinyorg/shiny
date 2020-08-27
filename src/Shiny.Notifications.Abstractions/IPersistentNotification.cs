using System;


namespace Shiny.Notifications
{
    public interface IPersistentNotification : IDisposable
    {
        string Title { get; set; }
        double? Progress { get; set; }
        void Dismiss();
    }
}

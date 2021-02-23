using System;


namespace Shiny.Notifications
{
    public interface IPersistentNotification : IDisposable
    {
        string Title { get; set; }
        string Message { get; set; }

        bool IsIndeterministic { get; }
        int? Total { get; set; }
        int? Progress { get; set; }

        void Show();
        void Dismiss();
    }
}

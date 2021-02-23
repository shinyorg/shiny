using System;


namespace Shiny.Notifications
{
    public interface IPersistentNotification : IDisposable
    {
        string Title { get; set; }
        string Message { get; set; }

        bool IsShowing { get; }
        bool? IsIndeterministic { get; set; }
        int Total { get; set; }
        int Progress { get; set; }

        void Show();
        void Dismiss();
    }
}

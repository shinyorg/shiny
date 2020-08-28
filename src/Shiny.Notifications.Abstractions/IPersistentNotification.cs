using System;


namespace Shiny.Notifications
{
    public interface IPersistentNotification : IDisposable
    {
        void SetIndeterministicProgress(bool show);
        void ClearProgress();
        void SetProgress(int progress, int total);
        void Dismiss();
    }
}

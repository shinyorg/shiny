using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Support.Uwp
{
    public interface IUwpBridge
    {
        void Bridge(IBackgroundTaskInstance taskInstance);
    }
}

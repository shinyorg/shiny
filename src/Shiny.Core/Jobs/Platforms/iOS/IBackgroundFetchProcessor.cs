using System;
using UIKit;


namespace Shiny.Jobs
{
    public interface IBackgroundFetchProcessor
    {
        void Process(Action<UIBackgroundFetchResult> completionHandler);
    }
}

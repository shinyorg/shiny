using System;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public interface IBackgroundTaskProcessor
    {
        void Process(string taskName, IBackgroundTaskInstance taskInstance);
    }
}

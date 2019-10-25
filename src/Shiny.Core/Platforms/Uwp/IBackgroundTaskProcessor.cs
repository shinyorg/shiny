using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public interface IBackgroundTaskProcessor
    {
        void Process(string taskName, IBackgroundTaskInstance taskInstance);
    }
}

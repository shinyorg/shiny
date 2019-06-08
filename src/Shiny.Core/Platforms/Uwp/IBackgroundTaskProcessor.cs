using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public interface IBackgroundTaskProcessor
    {
        Task Process(IBackgroundTaskInstance taskInstance, CancellationToken cancelToken);
    }
}

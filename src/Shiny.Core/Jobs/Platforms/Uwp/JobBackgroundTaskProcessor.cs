using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;


namespace Shiny.Jobs
{
    public class JobBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        public Task Process(IBackgroundTaskInstance taskInstance, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }
    }
}

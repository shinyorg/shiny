using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    // https://docs.microsoft.com/en-us/xamarin/ios/watchos/platform/background-tasks
    public class JobManagerImpl : AbstractJobManager
    {
        public JobManagerImpl(IServiceProvider container, IRepository repository) : base(container, repository)
        {
        }


        public override Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        protected override void CancelNative(JobInfo jobInfo)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterNative(JobInfo jobInfo)
        {
            throw new NotImplementedException();
        }
    }
}

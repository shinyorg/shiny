using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManagerImpl : AbstractJobManager
    {
        public JobManagerImpl(IServiceProvider container, IRepository repository, ILogger<IJobManager> logger) : base(container, repository, logger)
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

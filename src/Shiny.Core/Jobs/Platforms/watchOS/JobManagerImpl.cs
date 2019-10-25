using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs
{
    public class JobManagerImpl : AbstractJobManager
    {
        public JobManagerImpl(IServiceProvider container,
                              IRepository repository,
                              IPowerManager powerManager,
                              IConnectivity connectivity) : base(container, repository, powerManager, connectivity, null)
        {
        }


        public override Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}

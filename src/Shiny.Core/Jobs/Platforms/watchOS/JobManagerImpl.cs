using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobManagerImpl : AbstractJobManager
    {
        public JobManagerImpl(IServiceProvider container, IRepository repository) : base(container, repository, null)
        {
        }


        public override Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}

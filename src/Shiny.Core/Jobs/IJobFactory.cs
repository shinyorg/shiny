using System;


namespace Shiny.Jobs
{
    public interface IJobFactory
    {
        IJob Resolve(JobInfo jobInfo);
    }
}

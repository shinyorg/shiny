using System;


namespace Shiny.Jobs
{
    public class JobManagerConfig
    {
        public TimeSpan PeriodInterval { get; set; } = TimeSpan.FromMinutes(10);
    }
}

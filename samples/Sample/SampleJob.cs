using Shiny.Jobs;

namespace Sample;


public class SampleJob : IJob
{
    public Task Run(JobInfo jobInfo, CancellationToken cancelToken) => Task.CompletedTask;
}


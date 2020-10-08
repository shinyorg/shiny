using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Shiny.Generators.Tests
{
    public class TestJob : IJob
    {
        public Task Run(JobInfo jobInfo, CancellationToken cancelToken) => throw new NotImplementedException();
    }
}

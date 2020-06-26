using System;
using System.Threading;
using Microsoft.Extensions.Hosting;


namespace Shiny.Hosting
{
    public class HostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => throw new NotImplementedException();

        public CancellationToken ApplicationStopping => throw new NotImplementedException();

        public CancellationToken ApplicationStopped => throw new NotImplementedException();

        public void StopApplication()
        {
            throw new NotImplementedException();
        }
    }
}
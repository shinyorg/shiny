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
//internal class LifetimeEventsHostedService : IHostedService
//{
//    private readonly ILogger _logger;
//    private readonly IHostApplicationLifetime _appLifetime;

//    public LifetimeEventsHostedService(
//        ILogger<LifetimeEventsHostedService> logger,
//        IHostApplicationLifetime appLifetime)
//    {
//        _logger = logger;
//        _appLifetime = appLifetime;
//    }

//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        _appLifetime.ApplicationStarted.Register(OnStarted);
//        _appLifetime.ApplicationStopping.Register(OnStopping);
//        _appLifetime.ApplicationStopped.Register(OnStopped);

//        return Task.CompletedTask;
//    }

//    public Task StopAsync(CancellationToken cancellationToken)
//    {
//        return Task.CompletedTask;
//    }

//    private void OnStarted()
//    {
//        _logger.LogInformation("OnStarted has been called.");

//        // Perform post-startup activities here
//    }

//    private void OnStopping()
//    {
//        _logger.LogInformation("OnStopping has been called.");

//        // Perform on-stopping activities here
//    }

//    private void OnStopped()
//    {
//        _logger.LogInformation("OnStopped has been called.");

//        // Perform post-stopped activities here
//    }
//}
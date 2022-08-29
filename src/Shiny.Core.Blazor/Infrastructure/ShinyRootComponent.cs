using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Infrastructure;


public class ShinyRootComponent : ComponentBase
{
    [Inject]
    public IServiceProvider Services { get; set; } = null!;

    [Inject]
    public ILogger<ShinyRootComponent> Logger { get; set; } = null!;


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var tasks = this.Services.GetServices<IShinyStartupTask>();
        if (tasks.Any())
        {
            this.Logger.LogInformation($"Running {tasks.Count()} Shiny Startup Tasks");

            foreach (var task in tasks)
            {
                try
                {
                    task.Start();
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, $"Failed to start '{task.GetType().FullName}'");
                }
            }
        }

        var services = this.Services.GetServices<IShinyWebAssemblyService>();
        if (services.Any())
        {
            this.Logger.LogInformation($"Starting '{services.Count()}' Shiny WASM Services");

            var startups = new List<Task>();
            foreach (var service in services)
                startups.Add(this.Execute(service));

            await Task.WhenAll(startups);
        }
    }

    async Task Execute(IShinyWebAssemblyService service)
    {
        try
        {
            await service.OnStart();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, $"ShinyWasmService '{service.GetType().FullName}' failed to start");
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Hosting;
using Shiny.Stores;

namespace Shiny.Infrastructure;


public class ShinyRootComponent : ComponentBase
{
    [Inject] public IServiceProvider Services { get; set; } = null!;
    [Inject] public ILoggerFactory LoggerFactory { get; set; } = null!;
    [Inject] public ILogger<ShinyRootComponent> Logger { get; set; } = null!;


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var jsRuntime = this.Services.GetRequiredService<IJSRuntime>() as IJSInProcessRuntime;
        if (jsRuntime == null)
            throw new InvalidProgramException("IJSRuntime is not an in-proc (webassembly IJSInProcessRuntime) or is not in the dependency container");

        // keystores must be initialized first to allow all other services to bind properly
        var services = this.Services
            .GetServices<IKeyValueStore>()
            .OfType<IShinyWebAssemblyService>()
            .ToList();

        await this.IterateServices(services, jsRuntime);

        services = this.Services
            .GetServices<IShinyWebAssemblyService>()
            .Where(x => x is not IKeyValueStore)
            .ToList();

        await this.IterateServices(services, jsRuntime);

        var host = new Host(this.Services, this.LoggerFactory);
        host.Run(); // setup default shiny host
    }


    async Task IterateServices(IEnumerable<IShinyWebAssemblyService> services, IJSInProcessRuntime jsRuntime)
    {
        if (services.Any())
        {
            this.Logger.LogInformation($"Starting '{services.Count()}' Shiny WASM Services");

            var startups = new List<Task>();
            foreach (var service in services)
                startups.Add(this.Execute(service, jsRuntime));

            await Task.WhenAll(startups);
        }
    }


    async Task Execute(IShinyWebAssemblyService service, IJSInProcessRuntime jsRuntime)
    {
        try
        {
            await service.OnStart(jsRuntime);
            this.Logger.LogInformation($"Started up {service.GetType().FullName}");
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, $"ShinyWasmService '{service.GetType().FullName}' failed to start");
        }
    }
}


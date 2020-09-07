using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class DataSyncStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseDataSync<DataSyncDelegate>();
    }
}
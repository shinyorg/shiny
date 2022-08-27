using Shiny;
using BlazorSample;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.UseShiny();
builder.Services.AddConnectivity();
builder.Services.AddBattery();
//builder.Services.AddBluetoothLE();

var host = builder.Build();

// TODO
var tasks = host.Services.GetServices<IShinyStartupTask>();
foreach (var task in tasks)
    task.Start();

await host.RunAsync();

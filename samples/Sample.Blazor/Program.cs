using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Shiny;
using Shiny.Push.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddConnectivity();
builder.Services.AddBattery();
builder.Services.AddBluetoothLE();
builder.Services.AddGps();
// no browser geocoding API - this registers the OpenStreetMap Nominatim geocoder
builder.Services.AddGeocoding();
builder.Services.AddBlazorHttpTransfers<SampleHttpTransferDelegate>();
builder.Services.AddScreenRecorder();

// Thermal ESC/POS over Web Bluetooth / Web Serial / WebUSB, plus the OS print dialog via window.print()
builder.Services.AddBrowserPrinting();
builder.Services.AddBlazorPrinting();
builder.Services.AddScoped<Sample.Blazor.Printing.BrowserImageDecoder>();
builder.Services.AddPush<SamplePushDelegate>(new WebPushOptions
{
    // Replace with your own VAPID public key generated for your push backend
    VapidPublicKey = "BNbxGYNMhEIi9zrneh7mqV4oUanjLUK3m-REPLACE-ME"
});
var host = builder.Build();
await host.Services.UseShiny();
await host.RunAsync();

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Shiny;
using Shiny.Push.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBluetoothLE();
builder.Services.AddGps();
builder.Services.AddBlazorHttpTransfers<SampleHttpTransferDelegate>();
builder.Services.AddPush<SamplePushDelegate>(new WebPushOptions
{
    // Replace with your own VAPID public key generated for your push backend
    VapidPublicKey = "BNbxGYNMhEIi9zrneh7mqV4oUanjLUK3m-REPLACE-ME"
});
await builder.Build().RunAsync();

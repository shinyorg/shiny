using Sample;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.UseShiny();
//builder.Services.AddGps();
//builder.Services.AddConnectivity();
//builder.Services.AddBattery();
//builder.Services.AddBluetoothLE();
//builder.Services.AddSpeechRecognition();

var host = builder.Build();

await host.RunAsync();
using Acr.UserDialogs;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Tests;


public abstract class AbstractShinyTests : IDisposable
{
    readonly ITestOutputHelper output;


    protected AbstractShinyTests(ITestOutputHelper output)
    {
        this.output = output;
        var builder = new HostBuilder();

        builder.Logging.AddXUnit(output);
        this.Configure(builder);
        this.Host = builder.Build();
        this.Host.Run(); // force Host.Current to be hooked
    }


    protected virtual void Log(string message) => this.output.WriteLine(message);
    protected T GetService<T>() => this.Host!.Services!.GetRequiredService<T>()!;
    protected IHost Host { get; }
    protected virtual void Configure(IHostBuilder hostBuilder) { }
    protected virtual Task Alert(string message) => UserDialogs.Instance.AlertAsync(message);

    public virtual void Dispose() => this.Host?.Dispose();
}

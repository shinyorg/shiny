using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Tests;


public abstract class AbstractShinyTests : IDisposable
{
    readonly ITestOutputHelper output;


    protected AbstractShinyTests(ITestOutputHelper output)
    {
        this.output = output;
        var builder = HostBuilder.Create();

        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddXUnit(output);
        builder.Logging.AddDebug();

        this.Configure(builder);
        
        this.Host = builder.Build();
        this.Host.Run(); // force Host.Current to be hooked
    }


    protected virtual CompositeDisposable Disposable { get; } = new();
    protected virtual void Log(string message) => this.output.WriteLine(message);
    protected T GetService<T>() => this.Host!.Services!.GetRequiredService<T>()!;
    protected IHost Host { get; }
    protected virtual void Configure(HostBuilder hostBuilder) { }


    public virtual void Dispose()
    {
        this.Disposable.Dispose();
        this.Host?.Dispose();   
    }
}

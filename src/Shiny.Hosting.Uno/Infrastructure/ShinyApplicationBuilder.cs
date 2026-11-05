using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Uno.Extensions.Hosting;

namespace Shiny.Infrastructure;


/// <summary>
/// The <see cref="IApplicationBuilder"/> Shiny hands to <see cref="IShinyUnoApplication.ConfigureApp"/>. It does what
/// Uno's own builder does - <see cref="UnoHost.CreateDefaultBuilder(Assembly, string[])"/> plus the configured
/// delegates - with two differences that let it be built before Uno has created the App or a window, which on Android
/// is the only way the host exists when the OS starts the process in the background:
/// <list type="bullet">
/// <item><see cref="App"/> and <see cref="Arguments"/> are filled in by <c>OnLaunched</c> (through
/// <c>GetShinyBuilder</c>), and <see cref="Window"/> is created on first use rather than with the builder.</item>
/// <item><see cref="Build"/> builds once - <c>NavigateAsync</c> calls it again after the Android path already has, and
/// gets the same host back.</item>
/// </list>
/// </summary>
sealed class ShinyApplicationBuilder(Assembly applicationAssembly) : IApplicationBuilder
{
    readonly object sync = new();
    readonly List<Action<IHostBuilder>> hostDelegates = [];
    readonly List<Action<IHostBuilder, Window>> windowDelegates = [];
    Application? app;
    LaunchActivatedEventArgs? arguments;
    Window? window;
    IHost? host;


    public Application App => this.app ?? throw new InvalidOperationException(
        "The Uno App does not exist yet - on Android Shiny creates this builder in Application.OnCreate, before Uno creates the App. It is set once OnLaunched calls GetShinyBuilder."
    );

    // null until OnLaunched passes its arguments through GetShinyBuilder
    public LaunchActivatedEventArgs Arguments => this.arguments!;

    public Window Window
    {
        get
        {
            lock (this.sync)
                return this.window ??= new Window();
        }
    }

    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();


    public IApplicationBuilder Configure(Action<IHostBuilder> configureHost)
    {
        this.hostDelegates.Add(configureHost);
        return this;
    }


    public IApplicationBuilder Configure(Action<IHostBuilder, Window> configureHost)
    {
        this.windowDelegates.Add(configureHost);
        return this;
    }


    [RequiresDynamicCode("Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]")]
    [RequiresUnreferencedCode("Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]")]
    public IHost Build()
    {
        lock (this.sync)
        {
            if (this.host != null)
                return this.host;

            if (this.windowDelegates.Count > 0 && this.app == null)
                throw new InvalidOperationException(
                    "Configure((host, window) => ...) cannot be used with Shiny on Android: the host is built in Application.OnCreate, before Uno has started or created a window. Use Configure(host => ...) - Uno.Extensions navigation attaches the Window to the services itself, as a scoped Window you can resolve."
                );

            var builder = UnoHost.CreateDefaultBuilder(applicationAssembly, Environment.GetCommandLineArgs().Skip(1).ToArray());
            foreach (var configure in this.hostDelegates)
                configure(builder);

            foreach (var configure in this.windowDelegates)
                configure(builder, this.Window);

            this.host = builder.Build();
            return this.host;
        }
    }


    internal void SetLaunch(Application application, LaunchActivatedEventArgs? args)
    {
        lock (this.sync)
        {
            this.app ??= application;
            this.arguments ??= args;
        }
    }
}

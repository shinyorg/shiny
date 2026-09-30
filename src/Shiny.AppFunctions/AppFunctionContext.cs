namespace Shiny.AppFunctions;

public enum AppFunctionPlatform
{
    /// <summary>Tests, or an in-process caller such as an AI tool adapter.</summary>
    Other,
    /// <summary>Siri, Shortcuts, Spotlight or Apple Intelligence through App Intents.</summary>
    Apple,
    /// <summary>Gemini or another agent through Android AppFunctions.</summary>
    Android
}

/// <summary>Where a call came from. Built by the platform layer (or by you, when calling the dispatcher directly).</summary>
public sealed record AppFunctionInvocation(
    string FunctionId,
    AppFunctionPlatform Platform = AppFunctionPlatform.Other,
    bool IsForeground = false,
    string? CallerPackage = null
);

/// <summary>Per-invocation information handed to handlers and delegates.</summary>
public sealed class AppFunctionContext
{
    internal AppFunctionContext(AppFunctionDescriptor function, AppFunctionInvocation invocation, IServiceProvider services)
    {
        this.Function = function;
        this.Invocation = invocation;
        this.Services = services;
    }

    public AppFunctionDescriptor Function { get; }
    public AppFunctionInvocation Invocation { get; }
    public string FunctionId => this.Function.Id;
    public AppFunctionPlatform Platform => this.Invocation.Platform;

    /// <summary>
    /// The app is on screen for this run: the user was already in it when they asked Siri or Gemini, or (iOS) it was
    /// brought forward by <see cref="AppFunctionAttribute.OpensApp"/> or <see cref="AppFunctionGate.OpenApp"/>.
    /// The handler still runs off the main thread, so dispatch UI work to it.
    /// </summary>
    public bool IsForeground => this.Invocation.IsForeground;

    /// <summary>Android: the package name of the calling agent. Empty when called from the shell.</summary>
    public string? CallerPackage => this.Invocation.CallerPackage;

    /// <summary>The typed request record. Null for entity queries.</summary>
    public object? Request { get; internal set; }

    /// <summary>The invocation's DI scope.</summary>
    public IServiceProvider Services { get; }

    /// <summary>State shared between delegates and the handler for this invocation.</summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>Text for Siri to show or speak, and for Android to return alongside the result.</summary>
    public string? Dialog { get; private set; }

    public void Say(string dialog) => this.Dialog = dialog;
}

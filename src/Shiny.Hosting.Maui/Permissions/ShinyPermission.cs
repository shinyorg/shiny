using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using MauiPermissionException = Microsoft.Maui.ApplicationModel.PermissionException;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace Shiny;


/// <summary>
/// Base for the <see cref="ShinyPermissions"/> checks - a MAUI <see cref="MauiPermissions.BasePermission"/>, so every
/// one of them works with <c>Permissions.CheckStatusAsync&lt;T&gt;()</c> / <c>Permissions.RequestAsync&lt;T&gt;()</c>.
/// </summary>
/// <remarks>
/// Each check asks the operating system directly - the Shiny module itself does not need to be referenced or
/// registered. On Android the Shiny host must be running (<c>UseShiny()</c>), because the request goes through
/// Shiny's own permission plumbing.
/// <list type="bullet">
/// <item>A platform or device that cannot do the feature at all throws <see cref="FeatureNotSupportedException"/>.</item>
/// <item>A missing manifest, Info.plist or capability entry throws <see cref="MauiPermissionException"/>.</item>
/// <item>A partial grant (approximate location, limited contacts, notifications off for a foreground service)
/// reports <see cref="PermissionStatus.Limited"/>.</item>
/// </list>
/// </remarks>
public abstract partial class ShinyPermission : MauiPermissions.BasePermission
{
    public override Task<PermissionStatus> CheckStatusAsync()
    {
        this.EnsureDeclared();
        return this.GetStatus();
    }


    public override Task<PermissionStatus> RequestAsync()
    {
        this.EnsureDeclared();
        return this.Request();
    }


    public override void EnsureDeclared() => this.EnsurePlatformDeclared();


    public override bool ShouldShowRationale() => this.ShouldShowPlatformRationale();


    /// <summary>
    /// Reads the current state without prompting. Defaults to <see cref="GetAccessState"/>.
    /// </summary>
    protected virtual async Task<PermissionStatus> GetStatus()
    {
        var state = await this.GetAccessState().ConfigureAwait(false);
        return this.ToStatus(state);
    }


    /// <summary>
    /// Prompts where the platform needs to, and returns the result. Defaults to <see cref="RequestAccessState"/>.
    /// </summary>
    protected virtual async Task<PermissionStatus> Request()
    {
        var state = await this.RequestAccessState().ConfigureAwait(false);
        return this.ToStatus(state);
    }


    /// <summary>
    /// The current state as a Shiny <see cref="AccessState"/>, where <see cref="AccessState.Restricted"/> is a partial
    /// grant. Override <see cref="GetStatus"/> instead where the platform has a restricted state of its own.
    /// </summary>
    protected virtual Task<AccessState> GetAccessState() => Task.FromResult(AccessState.NotSupported);


    /// <summary>
    /// Prompts and returns a Shiny <see cref="AccessState"/>. Defaults to <see cref="GetAccessState"/> - a platform
    /// with nothing to ask for.
    /// </summary>
    protected virtual Task<AccessState> RequestAccessState() => this.GetAccessState();


    /// <summary>
    /// Maps a Shiny <see cref="AccessState"/>: <see cref="AccessState.Restricted"/> (a partial grant) is
    /// <see cref="PermissionStatus.Limited"/>, <see cref="AccessState.NotSupported"/> throws
    /// <see cref="FeatureNotSupportedException"/> and <see cref="AccessState.NotSetup"/> throws
    /// <see cref="MauiPermissionException"/>.
    /// </summary>
    protected PermissionStatus ToStatus(AccessState state) => state switch
    {
        AccessState.Available => PermissionStatus.Granted,
        AccessState.Restricted => PermissionStatus.Limited,
        AccessState.Denied => PermissionStatus.Denied,
        AccessState.Disabled => PermissionStatus.Disabled,
        AccessState.NotSupported => throw this.NotSupported(),
        AccessState.NotSetup => throw new MauiPermissionException($"{this.GetType().Name} is not set up - check the app's manifest, Info.plist or capabilities"),
        _ => PermissionStatus.Unknown
    };


    protected FeatureNotSupportedException NotSupported()
        => new($"{this.GetType().Name} is not supported on this platform or device");


#if !ANDROID && !APPLE && !WINDOWS
    void EnsurePlatformDeclared() { }
    bool ShouldShowPlatformRationale() => false;
#endif
}

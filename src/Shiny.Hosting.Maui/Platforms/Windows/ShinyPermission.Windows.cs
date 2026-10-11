using System;
using Windows.Security.Authorization.AppCapabilityAccess;
using MauiPermissionException = Microsoft.Maui.ApplicationModel.PermissionException;

namespace Shiny;


public abstract partial class ShinyPermission
{
    static readonly Lazy<bool> isPackaged = new(() =>
    {
        try
        {
            _ = Windows.ApplicationModel.Package.Current.Id;
            return true;
        }
        catch
        {
            return false;
        }
    });


    /// <summary>
    /// Whether the app runs with package identity - capabilities only apply to a packaged app.
    /// </summary>
    protected internal static bool IsPackaged => isPackaged.Value;


    /// <summary>
    /// The privacy-controlled capabilities (<c>location</c>, <c>appointments</c>, <c>wiFiControl</c>, ...) this check
    /// needs declared in Package.appxmanifest.
    /// </summary>
    protected virtual string[] WindowsCapabilities => [];


    void EnsurePlatformDeclared()
    {
        if (!IsPackaged)
            return;

        foreach (var capability in this.WindowsCapabilities)
        {
            if (AppCapability.Create(capability).CheckAccess() == AppCapabilityAccessStatus.NotDeclaredByApp)
                throw new MauiPermissionException($"You need to declare the `{capability}` capability in your Package.appxmanifest");
        }
    }


    bool ShouldShowPlatformRationale() => false;


    /// <summary>
    /// Reads a privacy-controlled capability's state without prompting. The system-wide switch being off is
    /// <see cref="AccessState.Disabled"/>.
    /// </summary>
    protected internal static AccessState GetCapabilityState(string capability) => AppCapability.Create(capability).CheckAccess() switch
    {
        AppCapabilityAccessStatus.Allowed => AccessState.Available,
        AppCapabilityAccessStatus.DeniedByUser => AccessState.Denied,
        AppCapabilityAccessStatus.DeniedBySystem => AccessState.Disabled,
        AppCapabilityAccessStatus.NotDeclaredByApp => IsPackaged ? AccessState.NotSetup : AccessState.Unknown,
        _ => AccessState.Unknown
    };
}

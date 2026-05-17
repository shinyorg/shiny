using System;
using System.Collections.Generic;

namespace Shiny.Jobs;


/// <summary>
/// Describes a scheduled job and the constraints under which it may run.
/// </summary>
/// <param name="Identifier">Unique identifier of the job.</param>
/// <param name="JobTypeName">Full name of the CLR type implementing the job.</param>
/// <param name="RunOnForeground">When true, the job is allowed to run while the application is in the foreground.</param>
/// <param name="Parameters">Optional key/value parameters passed to the job at runtime.</param>
/// <param name="RequiredInternetAccess">The internet access required for the job to fire.</param>
/// <param name="DeviceCharging">When true, the job only runs while the device is charging.</param>
/// <param name="BatteryNotLow">When true, the job only runs if the device battery is not low.</param>
/// <param name="IsSystemJob">Indicates the job is a Shiny system job rather than user-registered.</param>
public record JobInfo(
    string Identifier,
    string JobTypeName,
    bool RunOnForeground = false,
    Dictionary<string, string>? Parameters = null,
    InternetAccess RequiredInternetAccess = InternetAccess.None,
    bool DeviceCharging = false,
    bool BatteryNotLow = false,
    bool IsSystemJob = false
)
{
    readonly bool valid = Identifier.IsEmpty()
        ? throw new InvalidOperationException("Identifier is not set")
        : true;
}

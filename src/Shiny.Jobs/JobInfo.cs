using System;
using System.Collections.Generic;

namespace Shiny.Jobs;


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
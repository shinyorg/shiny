using System;
using System.Collections.Generic;
using Shiny.Support.Repositories;

namespace Shiny.Jobs;


public record JobInfo(
    string Identifier,
    Type JobType, // TODO: I need to be able to deal with null here
    bool RunOnForeground = false,
    Dictionary<string, string>? Parameters = null,
    InternetAccess RequiredInternetAccess = InternetAccess.None,
    bool DeviceCharging = false,
    bool BatteryNotLow = false,
    bool IsSystemJob = false
) : IRepositoryEntity
{
    readonly bool valid = Check.Assert(Identifier, JobType);

    internal static class Check
    {
        internal static bool Assert(string identifier, Type jobType)
        {
            if (identifier.IsEmpty())
                throw new InvalidOperationException("Identifier is not set");

            //if (jobType == null)
            //    throw new ArgumentException("Job type is null");

            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using Shiny.Stores.Infrastructure;

namespace Shiny.Jobs.Infrastructure;


public class JobInfoStoreConverter : IStoreConverter<JobInfo>
{
    public JobInfo FromStore(IDictionary<string, object> values)
    {
        //
        var info = new JobInfo((string)values["TypeName"], (string)values["Identifier"])
        {

        };
        //    var info = new JobInfo(job.TypeName!, job.Identifier!)
        //    {
        //        IsSystemJob = job.IsSystemJob,
        //        LastRunUtc = job.LastRunUtc,
        //        Repeat = job.Repeat,
        //        DeviceCharging = job.DeviceCharging,
        //        BatteryNotLow = job.BatteryNotLow,
        //        RequiredInternetAccess = job.RequiredInternetAccess,
        //        RunOnForeground = job.RunOnForeground,
        //        Parameters = job.Parameters
        //    };
        //    if (job.PeriodicTimeSeconds != null)
        //        info.PeriodicTime = TimeSpan.FromSeconds(job.PeriodicTimeSeconds.Value);
        if (values.ContainsKey(nameof(JobInfo.PeriodicTime)))
        {
            var time = (long)values[nameof(JobInfo.PeriodicTime)];
            info.PeriodicTime = TimeSpan.FromSeconds(time);
        }

        return info;
    }


    public IEnumerable<(string Property, object value)> ToStore(JobInfo entity)
    {
        yield return (nameof(JobInfo.Identifier), entity.Identifier);

        //    Identifier = job.Identifier,
        //    TypeName = job.TypeName,
        //    PeriodicTimeSeconds = job.PeriodicTime?.TotalSeconds,
        //    IsSystemJob = job.IsSystemJob,
        //    Repeat = job.Repeat,
        //    LastRunUtc = job.LastRunUtc,
        //    DeviceCharging = job.DeviceCharging,
        //    BatteryNotLow = job.BatteryNotLow,
        //    RequiredInternetAccess = job.RequiredInternetAccess,
        //    RunOnForeground = job.RunOnForeground,
        //    Parameters = job.Parameters

        if (entity.PeriodicTime != null)
            yield return (nameof(JobInfo.PeriodicTime), entity.PeriodicTime.Value.TotalSeconds);
    }
}

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
            IsSystemJob = (bool)values[nameof(JobInfo.IsSystemJob)],
            RunOnForeground = (bool)values[nameof(JobInfo.RunOnForeground)],
            Repeat = (bool)values[nameof(JobInfo.Repeat)],
        };
        //        LastRunUtc = job.LastRunUtc,
        //        DeviceCharging = job.DeviceCharging,
        //        BatteryNotLow = job.BatteryNotLow,
        //        RequiredInternetAccess = job.RequiredInternetAccess,
        //        RunOnForeground = job.RunOnForeground,
        //        Parameters = job.Parameters
        if (values.ContainsKey(nameof(JobInfo.PeriodicTime)))
        {
            var time = (double)values[nameof(JobInfo.PeriodicTime)];
            info.PeriodicTime = TimeSpan.FromSeconds(time);
        }

        return info;
    }


    public IEnumerable<(string Property, object value)> ToStore(JobInfo entity)
    {
        yield return (nameof(JobInfo.TypeName), entity.TypeName);
        yield return (nameof(JobInfo.Identifier), entity.Identifier);
        yield return (nameof(JobInfo.IsSystemJob), entity.IsSystemJob);
        yield return (nameof(JobInfo.Repeat), entity.Repeat);
        yield return (nameof(JobInfo.RequiredInternetAccess), entity.RequiredInternetAccess);
        yield return (nameof(JobInfo.RunOnForeground), entity.RunOnForeground);
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

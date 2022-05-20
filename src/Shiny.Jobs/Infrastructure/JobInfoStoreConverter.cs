using System;
using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Jobs.Infrastructure;


public class JobInfoStoreConverter : IStoreConverter<JobInfo>
{
    public JobInfo FromStore(IDictionary<string, object> values)
    {
        var info = new JobInfo((string)values[nameof(JobInfo.TypeName)], (string)values[nameof(JobInfo.Identifier)])
        {
            IsSystemJob = (bool)values[nameof(JobInfo.IsSystemJob)],
            RunOnForeground = (bool)values[nameof(JobInfo.RunOnForeground)],
            Repeat = (bool)values[nameof(JobInfo.Repeat)],
            DeviceCharging = (bool)values[nameof(JobInfo.DeviceCharging)],
            BatteryNotLow = (bool)values[nameof(JobInfo.BatteryNotLow)],
            RequiredInternetAccess = (InternetAccess)values[nameof(JobInfo.RequiredInternetAccess)],
            Parameters = (Dictionary<string, object>)values[nameof(JobInfo.Parameters)]
        };

        if (values.ContainsKey(nameof(JobInfo.LastRunUtc)))
        {
            var unixMillis = (long)values[nameof(JobInfo.LastRunUtc)];
            info.LastRunUtc = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis).UtcDateTime;
        }

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
        yield return (nameof(JobInfo.DeviceCharging), entity.DeviceCharging);
        yield return (nameof(JobInfo.BatteryNotLow), entity.BatteryNotLow);
        yield return (nameof(JobInfo.RequiredInternetAccess), entity.RequiredInternetAccess);
        yield return (nameof(JobInfo.Parameters), entity.Parameters);

        if (entity.LastRunUtc != null)
            yield return (nameof(JobInfo.LastRunUtc), new DateTimeOffset(entity.LastRunUtc.Value).ToUnixTimeMilliseconds());

        if (entity.PeriodicTime != null)
            yield return (nameof(JobInfo.PeriodicTime), entity.PeriodicTime.Value.TotalSeconds);
    }
}

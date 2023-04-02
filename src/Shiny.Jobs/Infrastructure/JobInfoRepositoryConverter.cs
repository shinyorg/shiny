using System;
using System.Collections.Generic;
using Shiny.Stores;
using Shiny.Support.Repositories.Impl;

namespace Shiny.Jobs.Infrastructure;


public class JobInfoRepositoryConverter : RepositoryConverter<JobInfo>
{
    public override JobInfo FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var info = new JobInfo((string)values[nameof(JobInfo.TypeName)], (string)values[nameof(JobInfo.Identifier)])
        {
            IsSystemJob = (bool)values[nameof(JobInfo.IsSystemJob)],
            RunOnForeground = (bool)values[nameof(JobInfo.RunOnForeground)],
            Repeat = (bool)values[nameof(JobInfo.Repeat)],
            DeviceCharging = (bool)values[nameof(JobInfo.DeviceCharging)],
            BatteryNotLow = (bool)values[nameof(JobInfo.BatteryNotLow)],
            RequiredInternetAccess = (InternetAccess)(long)values[nameof(JobInfo.RequiredInternetAccess)],
            PeriodicTime = this.ConvertFromStoreValue<TimeSpan?>(values, nameof(JobInfo.PeriodicTime)),
            LastRun = this.ConvertFromStoreValue<DateTimeOffset?>(values, nameof(JobInfo.LastRun))
        };

        if (values.ContainsKey(nameof(JobInfo.Parameters)))
            info.Parameters = serializer.Deserialize<Dictionary<string, string>>((string)values[nameof(JobInfo.Parameters)])!;

        return info;
    }


    public override IEnumerable<(string Property, object Value)> ToStore(JobInfo entity, ISerializer serializer)
    {
        yield return (nameof(JobInfo.TypeName), entity.TypeName);
        yield return (nameof(JobInfo.IsSystemJob), entity.IsSystemJob);
        yield return (nameof(JobInfo.Repeat), entity.Repeat);
        yield return (nameof(JobInfo.RequiredInternetAccess), entity.RequiredInternetAccess);
        yield return (nameof(JobInfo.RunOnForeground), entity.RunOnForeground);
        yield return (nameof(JobInfo.DeviceCharging), entity.DeviceCharging);
        yield return (nameof(JobInfo.BatteryNotLow), entity.BatteryNotLow);

        if ((entity.Parameters?.Count ?? 0) > 0)
            yield return (nameof(JobInfo.Parameters), serializer.Serialize(entity.Parameters!));

        if (entity.LastRun != null)
            yield return (nameof(JobInfo.LastRun), this.ConvertToStoreValue(entity.LastRun));

        if (entity.PeriodicTime != null)
            yield return (nameof(JobInfo.PeriodicTime), this.ConvertToStoreValue(entity.PeriodicTime));
    }
}

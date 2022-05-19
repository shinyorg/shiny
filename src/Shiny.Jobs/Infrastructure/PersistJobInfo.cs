﻿using System;
using System.Collections.Generic;


namespace Shiny.Jobs.Infrastructure
{
    public class PersistJobInfo
    {
        public string? Identifier { get; set; }
        public string? TypeName { get; set; }
        public bool IsSystemJob { get; set; }
        public bool Repeat { get; set; }
        public bool BatteryNotLow { get; set; }
        public bool DeviceCharging { get; set; }
        public bool RunOnForeground { get; set; }
        public double? PeriodicTimeSeconds { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();


        public static JobInfo FromPersist(PersistJobInfo job)
        {
            var info = new JobInfo(job.TypeName!, job.Identifier!)
            {
                IsSystemJob = job.IsSystemJob,
                LastRunUtc = job.LastRunUtc,
                Repeat = job.Repeat,
                DeviceCharging = job.DeviceCharging,
                BatteryNotLow = job.BatteryNotLow,
                RequiredInternetAccess = job.RequiredInternetAccess,
                RunOnForeground = job.RunOnForeground,
                Parameters = job.Parameters
            };
            if (job.PeriodicTimeSeconds != null)
                info.PeriodicTime = TimeSpan.FromSeconds(job.PeriodicTimeSeconds.Value);
            return info;
        }


        public static PersistJobInfo ToPersist(JobInfo job) => new PersistJobInfo
        {
            Identifier = job.Identifier,
            TypeName = job.TypeName,
            PeriodicTimeSeconds = job.PeriodicTime?.TotalSeconds,
            IsSystemJob = job.IsSystemJob,
            Repeat = job.Repeat,
            LastRunUtc = job.LastRunUtc,
            DeviceCharging = job.DeviceCharging,
            BatteryNotLow = job.BatteryNotLow,
            RequiredInternetAccess = job.RequiredInternetAccess,
            RunOnForeground = job.RunOnForeground,
            Parameters = job.Parameters
        };
    }
}

using System;
using System.Collections.Generic;


namespace Shiny.Jobs.Infrastructure
{
    public class PersistJobInfo
    {
        public string? Identifier { get; set; }
        public string? TypeName { get; set; }
        public bool IsSystemJob { get; set; }
        public double PeriodicTimeMs { get; set; }
        public bool Repeat { get; set; }
        public bool BatteryNotLow { get; set; }
        public bool DeviceCharging { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();


        public void Assert()
        {
            if (Type.GetType(this.TypeName) == null)
                throw new ArgumentException("Job type not found - did you delete/move this class from your library?");
        }


        public static JobInfo FromPersist(PersistJobInfo job)
        {
            job.Assert();
            return new JobInfo(Type.GetType(job.TypeName), job.Identifier)
            {
                PeriodicTime = TimeSpan.FromMilliseconds(job.PeriodicTimeMs),
                IsSystemJob = job.IsSystemJob,
                LastRunUtc = job.LastRunUtc,
                Repeat = job.Repeat,
                DeviceCharging = job.DeviceCharging,
                BatteryNotLow = job.BatteryNotLow,
                RequiredInternetAccess = job.RequiredInternetAccess,
                Parameters = job.Parameters
            };
        }


        public static PersistJobInfo ToPersist(JobInfo job) => new PersistJobInfo
        {
            Identifier = job.Identifier,
            TypeName = job.Type.AssemblyQualifiedName,
            PeriodicTimeMs = job.PeriodicTime.TotalMilliseconds,
            IsSystemJob = job.IsSystemJob,
            Repeat = job.Repeat,
            LastRunUtc = job.LastRunUtc,
            DeviceCharging = job.DeviceCharging,
            BatteryNotLow = job.BatteryNotLow,
            RequiredInternetAccess = job.RequiredInternetAccess,
            Parameters = job.Parameters
        };
    }
}

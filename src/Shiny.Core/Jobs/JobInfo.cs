using System;
using System.Collections.Generic;


namespace Shiny.Jobs
{
    public class JobInfo
    {
        public JobInfo(Type jobType, string? identifier = null)
        {
            this.Type = jobType;
            this.Identifier = identifier ?? jobType.GetType().FullName;
        }


        public string Identifier { get; }
        public Type Type { get; }
        //public bool DeviceIdle { get; set; } // this will only work on droid

        public bool Repeat { get; set; } = true;
        public bool DeviceCharging { get; set; }
        public bool BatteryNotLow { get; set; }

        /// <summary>
        /// The desired time to run the job
        /// The minimum time is determined by IJobManager.Minimum
        /// It is suggested you use a value of 15 minutes.
        /// THIS VALUE DOES NOTHING ON IOS
        /// Minumum Values for:
        ///     UWP - 15 minutes
        ///     Android - 30 seconds
        /// </summary>
        public TimeSpan PeriodicTime { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Calling JobManager.Clear will not remove this task
        /// </summary>
        public bool IsSystemJob { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;
        public DateTime? LastRunUtc { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}

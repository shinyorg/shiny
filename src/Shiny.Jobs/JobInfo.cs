using System;
using System.Collections.Generic;


namespace Shiny.Jobs
{
    public class JobInfo
    {
        public JobInfo(Type jobType, string? identifier = null, bool runOnForeground = false)
        {
            if (jobType == null)
                throw new ArgumentException("Job Type not set");

            this.Identifier = identifier ?? jobType.AssemblyQualifiedName;
            this.TypeName = jobType.AssemblyQualifiedName;
            this.PeriodicTime = TimeSpan.FromMinutes(15);
            this.RunOnForeground = runOnForeground;
        }


        internal JobInfo(string typeName, string identifier)
        {
            this.TypeName = typeName;
            this.Identifier = identifier;
        }


        /// <summary>
        /// Periodic time works with Android & UWP. Though optional, you must provide some form of criteria for the platforms to trigger your job
        /// To prevent breaking changes, this is defaulted to 15 minutes which is the minimum value on UWP & Android
        /// </summary>
        public TimeSpan? PeriodicTime { get; set; }
        public string Identifier { get; }
        public string TypeName { get; }
        public bool Repeat { get; set; } = true;
        public bool DeviceCharging { get; set; }
        public bool BatteryNotLow { get; set; }
        public bool RunOnForeground { get; set; }

        /// <summary>
        /// Calling JobManager.Clear will not remove this task
        /// </summary>
        public bool IsSystemJob { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;
        public DateTime? LastRunUtc { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();


        public bool IsValid() => this.TypeName.IsEmpty() || Type.GetType(this.TypeName) != null;
    }
}

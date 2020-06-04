using System;

using Microsoft.Extensions.DependencyInjection;

using Shiny.Infrastructure;

namespace Shiny
{
    public abstract class ShinyLocationSyncAttribute : ServiceModuleAttribute
    {
        public ShinyLocationSyncAttribute(Type delegateType, int batchSize = 1, bool sortMostRecentFirst = false, bool requestPermissionOnStart = false)
        {
            this.DelegateType = delegateType;
            this.BatchSize = batchSize;
            this.SortMostRecentFirst = sortMostRecentFirst;
            this.RequestPermissionOnStart = requestPermissionOnStart;
        }


        public Type DelegateType { get; }
        public bool RequestPermissionOnStart { get; set; }
        public int BatchSize { get; set; }
        public bool SortMostRecentFirst { get; set; }


        protected SyncConfig GetConfig() => new SyncConfig
        {
            BatchSize = this.BatchSize,
            SortMostRecentFirst = this.SortMostRecentFirst
        };
    }


    public class ShinyGeofenceSyncAttribute : ShinyLocationSyncAttribute
    {
        public ShinyGeofenceSyncAttribute(Type delegateType, int batchSize = 1, bool sortMostRecentFirst = false, bool requestPermissionOnStart = false) 
            : base(delegateType, batchSize, sortMostRecentFirst, requestPermissionOnStart) {}

        public override void Register(IServiceCollection services)
        {
            var cfg = new SyncConfig
            {
                BatchSize = this.BatchSize,
                SortMostRecentFirst = this.SortMostRecentFirst
            };
            //services.UseGeofencingSync(this.DelegateType, cfg, this.RequestPermissionOnStart);
        }
    }


    public class ShinyGpsSyncAttribute : ShinyLocationSyncAttribute
    {
        public ShinyGpsSyncAttribute(Type delegateType, int batchSize = 1, bool sortMostRecentFirst = false, bool requestPermissionOnStart = false)
            : base(delegateType, batchSize, sortMostRecentFirst, requestPermissionOnStart) { }

        public override void Register(IServiceCollection services)
        {
            // gps request
            // sync config
            //services.UseGpsSync(this.DelegateType);
        }
    }
}

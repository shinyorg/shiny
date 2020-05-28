using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyPushAzureNotificationHubAttribute : ServiceModuleAttribute
    {
        public ShinyPushAzureNotificationHubAttribute(Type delegateType, string connectionString, string hubName)
        {
            this.DelegateType = delegateType;
            this.ConnectionString = connectionString;
            this.HubName = hubName;
        }


        public Type DelegateType { get; }
        public string ConnectionString { get; }
        public string HubName { get; }


        public override void Register(IServiceCollection services)
            => services.UsePushAzureNotificationHubs(this.DelegateType, this.ConnectionString, this.HubName);
    }
}

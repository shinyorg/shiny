using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public class ServiceRegisterAttribute : Attribute
    {
        public ServiceRegisterAttribute(Type serviceType, Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            this.ServiceType = serviceType;
            this.ImplementationType = implementationType;
        }

        public ServiceLifetime Lifetime { get; }
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
    }
}

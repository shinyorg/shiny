using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public class ShinyServiceAttribute : Attribute
    {
        public ShinyServiceAttribute(Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Singleton) : this(null, implementationType, lifetime) { }

        public ShinyServiceAttribute(Type serviceType, Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            this.ServiceType = serviceType;
            this.ImplementationType = implementationType;
            this.Lifetime = lifetime;
        }


        public ServiceLifetime Lifetime { get; }
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
    }
}

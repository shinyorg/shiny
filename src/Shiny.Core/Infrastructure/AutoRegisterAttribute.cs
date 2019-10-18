using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;


namespace Shiny.Infrastructure
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public abstract class AutoRegisterAttribute : Attribute
    {
        public abstract void Register(IServiceCollection services);


        static List<Type> typeCache;

        protected virtual Type FindImplementationType(Type delegateInterfaceType, bool required)
        {
            EnsureTypeCache();
            var implementationTypes = this.FindDelegateImplementationTypes(delegateInterfaceType);
            var first = implementationTypes.FirstOrDefault();

            switch (implementationTypes.Count)
            {
                case 0:
                    if (required)
                        throw new ArgumentException($"{this.GetType().Name} has a required delegate type implementation of {delegateInterfaceType.FullName} which was not found in your assemblies");
                    break;

                case 1:
                    Log.Write(
                        "AutoRegisterSuccess",
                        $"Implementation of {delegateInterfaceType.FullName} found for {this.GetType().Name} - {first.FullName}, {first.AssemblyQualifiedName}"
                    );
                    break;

                default:
                    Log.Write(
                        "AutoRegisterWarning",
                        $"Multiple implementations of {delegateInterfaceType.FullName} found for {this.GetType().Name} - registering first type {first.FullName}, {first.AssemblyQualifiedName}"
                    );
                    break;
            }
            return first;
        }


        protected virtual List<Type> FindDelegateImplementationTypes(Type delegateInterfaceType)
        {
            EnsureTypeCache();
            return typeCache
                .Where(delegateInterfaceType.IsAssignableFrom)
                .ToList();
        }


        static void EnsureTypeCache()
        {
            if (typeCache == null)
            { 
                typeCache = AssemblyQueries
                    .GetAssumedUserTypes()
                    .Where(typeof(IShinyDelegate).IsAssignableFrom)
                    .ToList();
            }
        }
    }
}

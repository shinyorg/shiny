using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Shiny.Infrastructure
{
    public static class AssemblyQueries
    {
        public static IEnumerable<Assembly> GetAssumedUserAssemblies() => AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(x =>
            {
                var name = x.GetName()?.FullName?.ToLower();
                if (name == null)
                    return false;

                if (name.Equals("shiny.test"))
                    return true;

                if (name.StartsWith("mscorlib"))
                    return false;

                if (name.StartsWith("shiny."))
                    return false;

                if (name.StartsWith("system."))
                    return false;

                if (name.StartsWith("xamarin."))
                    return false;

                return true;
            });


        public static IEnumerable<Assembly> GetShinyAssemblies() => AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(x => x.GetName()?.FullName.StartsWith("Shiny.", StringComparison.InvariantCultureIgnoreCase) ?? false);


        public static IEnumerable<Type> GetAssumedUserTypes() => GetAssumedUserAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsPublic && x.IsClass && !x.IsAbstract);
    }
}

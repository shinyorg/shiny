using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shiny.Jobs
{
    class DefaultJobFactory : IJobFactory
    {
        readonly IServiceProvider services;


        public DefaultJobFactory(IServiceProvider services)
        {
            this.services = services;
        }


        public IJob Resolve(JobInfo jobInfo)
        {
            var ctor = GetConstructor(jobInfo.Type);
            var args = new List<object>();

            foreach (var parameter in ctor.GetParameters())
            {
                var service = this.services.GetService(parameter.ParameterType);
                if (service != null)
                {
                    args.Add(service);
                }
                else
                {
                    if (!parameter.IsOptional)
                        throw new ArgumentException($"{parameter.Name} ({parameter.ParameterType.AssemblyQualifiedName}) is not optional and is not registered with the container");

                    args.Add(parameter.DefaultValue);
                }
            }

            var delegateInstance = (IJob)Activator.CreateInstance(jobInfo.Type, args.ToArray());
            return delegateInstance;
        }


        static ConstructorInfo GetConstructor(Type type)
        {
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (ctors.Length != 1)
                throw new ArgumentException("Delegate type must have only 1 public constructor");

            var ctor = ctors.FirstOrDefault();
            return ctor;
        }
    }
}
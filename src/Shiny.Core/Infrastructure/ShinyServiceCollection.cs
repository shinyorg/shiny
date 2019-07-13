using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Settings;

namespace Shiny.Infrastructure.DependencyInjection
{
    public class ShinyServiceCollection : IServiceCollection
    {
        readonly List<Action<IServiceProvider>> postBuildActions = new List<Action<IServiceProvider>>();
        readonly ServiceCollection services = new ServiceCollection();


        static bool ImplementsInterface<T>(ServiceDescriptor descriptor)
        {
            var typeName = typeof(T).FullName;
            var doesImpl = descriptor.ServiceType != null && descriptor.ServiceType.GetInterface(typeName) != null;
            if (!doesImpl)
                doesImpl = descriptor.ImplementationType != null && descriptor.ImplementationType.GetInterface(typeName) != null;

            return doesImpl;
        }


        internal void AddPostBuildAction(Action<IServiceProvider> action) => this.postBuildActions.Add(action);

        internal void RunPostBuildActions(IServiceProvider container)
        {
            foreach (var action in postBuildActions)
                action(container);

            postBuildActions.Clear();
        }


        public void Add(ServiceDescriptor service)
        {
            if (service.Lifetime != ServiceLifetime.Singleton)
            {
                this.services.Insert(0, service);
            }
            else if (service.ImplementationInstance is INotifyPropertyChanged npc)
            {
                this.services.Insert(0, new ServiceDescriptor(
                    service.ServiceType ?? npc.GetType(),
                    sp =>
                    {
                        sp.GetService<ISettings>().Bind(npc);
                        return npc;
                    },
                    service.Lifetime
                ));
            }
            else if (ImplementsInterface<INotifyPropertyChanged>(service))
            {
                var resolveType = service.ServiceType ?? service.ImplementationType;

                this.services.Insert(0, new ServiceDescriptor(
                    resolveType,
                    sp =>
                    {
                        var bindable = (INotifyPropertyChanged)sp.GetService(resolveType);
                        sp.GetService<ISettings>().Bind(bindable);
                        return bindable;
                    },
                    service.Lifetime
                ));
            }
            else
            {
                this.services.Insert(0, service);
            }

            if (ImplementsInterface<IShinyStartupTask>(service))
            {
                this.postBuildActions.Add(sp =>
                {
                    var resolveType = service.ServiceType ?? service.ImplementationType;
                    ((IShinyStartupTask)sp.GetService(resolveType)).Start();
                });
            }
        }


        public ServiceDescriptor this[int index]
        {
            get => this.services[index];
            set => throw new ArgumentException("NO");
        }

        public int Count => this.services.Count;
        public bool IsReadOnly => false;


        public void Clear() => this.services.Clear();
        public bool Contains(ServiceDescriptor item) => this.services.Contains(item);
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => this.services.CopyTo(array, arrayIndex);
        public IEnumerator<ServiceDescriptor> GetEnumerator() => this.services.GetEnumerator();
        public int IndexOf(ServiceDescriptor item) => this.services.IndexOf(item);
        public void Insert(int index, ServiceDescriptor item) => this.services.Insert(index, item);
        public bool Remove(ServiceDescriptor item) => this.services.Remove(item);
        public void RemoveAt(int index) => this.services.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

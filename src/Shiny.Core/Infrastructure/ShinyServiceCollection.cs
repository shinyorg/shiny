﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Settings;


namespace Shiny.Infrastructure.DependencyInjection
{
    public class ShinyServiceCollection : IServiceCollection
    {
        readonly ServiceCollection services = new ServiceCollection();


        public void RunPostBuildActions(IServiceProvider container)
        {
            var actions = ShinyHost.PostBuildActions.ToList();
            foreach (var action in actions)
                action(container);

            ShinyHost.PostBuildActions.Clear();
        }


        void AddItem(ServiceDescriptor descriptor) => ((ICollection<ServiceDescriptor>)this.services).Add(descriptor);


        public void Add(ServiceDescriptor service)
        {
            if (service.Lifetime != ServiceLifetime.Singleton)
            {
                this.AddItem(service);
            }
            else if (service.ImplementationInstance is INotifyPropertyChanged npc)
            {
                var resolveType = service.ServiceType ?? service.ImplementationInstance.GetType();

                this.services.AddSingleton(
                    resolveType,
                    sp =>
                    {
                        sp.Resolve<ISettings>(true).Bind(npc);
                        return npc;
                    }
                );
            }
            // TODO; this trap IPowerManager as well
            else if (Implements<INotifyPropertyChanged>(service))
            {
                var resolveType = service.ServiceType ?? service.ImplementationType;

                this.services.AddSingleton(
                    resolveType,
                    sp =>
                    {
                        var bindable = (INotifyPropertyChanged)ActivatorUtilities.CreateInstance(sp, service.ImplementationType);
                        sp.Resolve<ISettings>(true).Bind(bindable);
                        return bindable;
                    }
                );
            }
            else
            {
                this.AddItem(service);
            }

            if (service.ImplementationInstance is IShinyStartupTask task)
            {
                services.RegisterPostBuildAction(_ => task.Start());
            }
            else if (Implements<IShinyStartupTask>(service))
            {
                services.RegisterPostBuildAction(sp =>
                {
                    var resolveType = service.ServiceType ?? service.ImplementationType;
                    ((IShinyStartupTask)sp.GetService(resolveType)).Start();
                });
            }
        }


        static bool Implements<T>(ServiceDescriptor service)
        {
            if (service.ImplementationType == null)
                return false;

            var i = service.ImplementationType.GetInterface(typeof(T).FullName);
            return i != null;
        }


        static bool IsStartupTask(ServiceDescriptor service)
        {
            if (service.ImplementationType == null)
                return false;

            var i = service.ImplementationType.GetInterface(typeof(IShinyStartupTask).FullName);
            return i != null;
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

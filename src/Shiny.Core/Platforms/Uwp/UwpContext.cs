using System;
using System.Linq;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public class UwpContext
    {
        readonly IServiceProvider serviceProvider;


        public UwpContext(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }


        public void Bridge(IBackgroundTaskInstance task)
        {
            var type = Type.GetType(task.Task.Name);
            var processor = this.serviceProvider.ResolveOrInstantiate(type) as IBackgroundTaskProcessor;
            if (processor != null)
                processor.Process(task);
        }


        public void RegisterBackground<TService>(params IBackgroundTrigger[] triggers) where TService : IBackgroundTaskProcessor
        {
            var task = GetTask(typeof(TService));
            if (task != null)
                return;

            var builder = new BackgroundTaskBuilder
            {
                Name = typeof(TService).FullName,
                TaskEntryPoint = "Shiny.Support.Uwp.ShinyBackgroundTask"
            };
            foreach (var trigger in triggers)
                builder.SetTrigger(trigger);

            builder.Register();
        }


        public void UnRegisterBackground<TService>() where TService : IBackgroundTaskProcessor
            => GetTask(typeof(TService))?.Unregister(true);


        static IBackgroundTaskRegistration GetTask(Type serviceType) => BackgroundTaskRegistration
            .AllTasks
            .Where(x => x.Value.Name.Equals(serviceType.FullName))
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}

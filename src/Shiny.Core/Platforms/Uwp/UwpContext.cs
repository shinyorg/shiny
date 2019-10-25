using System;
using System.Linq;
using Shiny.Infrastructure;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public class UwpContext
    {
        static readonly string BackgroundTaskEntryPoint = typeof(Shiny.Support.Uwp.ShinyBackgroundTask).FullName;
        readonly IServiceProvider serviceProvider;
        readonly IRepository repository;


        public UwpContext(IServiceProvider serviceProvider, IRepository repository)
        {
            this.serviceProvider = serviceProvider;
            this.repository = repository;
        }


        public async void Bridge(IBackgroundTaskInstance task)
        {
            var register = await this.repository.Get<UwpTaskRegister>(task.Task.TaskId.ToString());
            var type = Type.GetType(register.DelegateTypeName);
            var processor = this.serviceProvider.ResolveOrInstantiate(type) as IBackgroundTaskProcessor;
            processor.Process(task.Task.Name, task);
        }


        public async void RegisterBackground<TService>(string taskIdentifier, Action<BackgroundTaskBuilder> builderAction = null) where TService : IBackgroundTaskProcessor
        {
            var task = GetTask(taskIdentifier, typeof(TService));
            if (task != null)
                return;

            var builder = new BackgroundTaskBuilder();
            builderAction?.Invoke(builder);
            builder.Name = taskIdentifier;
            builder.TaskEntryPoint = BackgroundTaskEntryPoint;

            var registration = builder.Register();
            await this.repository.Set(registration.TaskId.ToString(), new UwpTaskRegister
            {
                TaskId = registration.TaskId,
                TaskName = taskIdentifier,
                DelegateTypeName = typeof(TService).AssemblyQualifiedName
            });
        }


        public void UnRegisterBackground<TService>(string taskIdentifier) where TService : IBackgroundTaskProcessor
            => GetTask(taskIdentifier, typeof(TService))?.Unregister(true);


        static IBackgroundTaskRegistration GetTask(string taskIdentifier, Type serviceType) => BackgroundTaskRegistration
            .AllTasks
            .Where(x => x.Value.Name.Equals(taskIdentifier) && x.Value.Name.Equals(serviceType.FullName))
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}

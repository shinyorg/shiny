using System;

namespace Shiny.Hosting
{
    public interface ILifecycleBuilder
    {
        void OnStart();
        void OnStop();
        void On<T>(Func<T> action);

        ILifecycle Build();
    }
}

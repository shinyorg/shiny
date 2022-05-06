using System;

namespace Shiny.Hosting.Impl;

public class LifecycleBuilder : ILifecycleBuilder
{
    public ILifecycle Build()
    {
        throw new NotImplementedException();
    }

    public void On<T>(Func<T> action)
    {
        throw new NotImplementedException();
    }

    public void OnStart()
    {
        throw new NotImplementedException();
    }

    public void OnStop()
    {
        throw new NotImplementedException();
    }
}
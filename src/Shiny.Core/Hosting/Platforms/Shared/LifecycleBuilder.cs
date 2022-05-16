using System;
using System.Collections.Generic;

namespace Shiny.Hosting;

public class LifecycleBuilder : ILifecycleBuilder
{
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


    // what if I need return
    internal void Run<T>(T action)
    {

    }
}
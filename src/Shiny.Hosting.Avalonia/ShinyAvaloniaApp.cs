using System;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyAvaloniaApp : Avalonia.Application
{
    protected abstract IHost CreateShinyHost();


    public override void Initialize()
    {
        // TODO: how to wire up lifecycles?
        this.CreateShinyHost();
        base.Initialize();
    }
}


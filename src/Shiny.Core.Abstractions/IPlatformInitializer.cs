using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny
{
    public enum PlatformState
    {
        Foreground,
        Background
    }


    public interface IPlatformInitializer
    {
        //bool Is(string platform);
        //void InvokeOnMainThread(Action action);
        void Register(IServiceCollection services);
        IObservable<PlatformState> WhenStateChanged();
    }
}

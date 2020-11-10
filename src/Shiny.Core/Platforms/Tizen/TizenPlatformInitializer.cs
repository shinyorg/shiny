using System;
using System.Reactive.Linq;

using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public class TizenPlatformInitializer : IPlatformInitializer
    {
        public void Register(IServiceCollection services)
        {
            services.RegisterCommonServices();
        }


        public IObservable<PlatformState> WhenStateChanged() => Observable.Empty<PlatformState>();
    }
}

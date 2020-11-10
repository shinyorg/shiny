using System;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Testing
{
    public class TestPlatform : IPlatformInitializer
    {
        public virtual void Register(IServiceCollection services)
        {
        }


        public Subject<PlatformState> PlatformSubject { get; } = new Subject<PlatformState>();
        public virtual IObservable<PlatformState> WhenStateChanged()
            => this.PlatformSubject;
    }
}

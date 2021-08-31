using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Tests.Core.Infrastructure
{
    public class TestModule : ShinyModule
    {
        readonly Action? onRegister;
        readonly Action? onContainerReady;


        public TestModule(Action? onRegister = null, Action? onContainerReady = null)
        {
            this.onRegister = onRegister;
            this.onContainerReady = onContainerReady;
        }


        public override void Register(IServiceCollection services)
            => this.onRegister?.Invoke();


        public override void OnContainerReady(IServiceProvider services)
            => this.onContainerReady?.Invoke();
    }
}

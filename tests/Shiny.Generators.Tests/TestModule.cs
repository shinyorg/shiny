using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Generators.Tests
{
    public class TestModule : ShinyModule
    {
        public override void Register(IServiceCollection services) => throw new NotImplementedException();
    }
}

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny
{
    public interface IPlatformBuilder
    {
        void Register(IServiceCollection services);
    }
}

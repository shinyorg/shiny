using System;
using Microsoft.Extensions.Configuration;


namespace Shiny.Hosting
{
    public class AndroidConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new AndroidConfigurationProvider();
    }
}

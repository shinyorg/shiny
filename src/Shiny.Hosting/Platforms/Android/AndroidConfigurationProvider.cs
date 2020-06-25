using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;


namespace Shiny.Hosting
{
    public class AndroidConfigurationProvider : IConfigurationProvider
    {
        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            throw new NotImplementedException();
        }


        public IChangeToken GetReloadToken()
        {
            throw new NotImplementedException();
        }


        public void Load()
        {
            throw new NotImplementedException();
        }


        public void Set(string key, string value)
        {
            throw new NotImplementedException();
        }


        public bool TryGet(string key, out string value)
        {
            throw new NotImplementedException();
        }
    }
}

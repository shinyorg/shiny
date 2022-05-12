using System;
using System.Collections.Generic;
using System.Xml;


namespace Shiny.Stores
{
    public class AppConfigKeyValueStore : AbstractFileKeyValueStore
    {
        public AppConfigKeyValueStore(IPlatform platform) : base(platform, "appconfig", true) { }

        protected override IDictionary<string, object> Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Xml;


namespace Shiny.Stores
{
    public class PlistKeyValueStore : AbstractFileKeyValueStore
    {
        public PlistKeyValueStore(IPlatform platform) : base(platform, "plist", true)
            => this.FileName = "Info.plist";

        protected override IDictionary<string, object> Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            return null;
        }
    }
}

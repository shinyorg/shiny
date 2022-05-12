using System;
using System.Collections.Generic;
using System.IO;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public class FileKeyValueStore : AbstractFileKeyValueStore
    {
        readonly ISerializer serializer;


        public FileKeyValueStore(IPlatform platform, ISerializer serializer) : base(platform, "file", false)
        {
            this.serializer = serializer;
            this.FileName = "shinystore";
        }


        protected override void Store(string path, IDictionary<string, object> values)
        {
            var s = this.serializer.Serialize(values);
            File.WriteAllText(path, s);
        }


        protected override IDictionary<string, object> Load(string path)
        {
            var contents = File.ReadAllText(path);
            return this.serializer.Deserialize<Dictionary<string, object>>(contents) ?? new Dictionary<string, object>();
        }
    }
}

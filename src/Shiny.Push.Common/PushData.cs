using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shiny.Push
{
    public class PushData
    {
        public PushData(IDictionary<string, string> data)
            => this.Data = new ReadOnlyDictionary<string, string>(data);


        public IReadOnlyDictionary<string, string> Data { get; }
    }
}

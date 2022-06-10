using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Net.Http;


public class HttpTransferStoreConverter : IStoreConverter<HttpTransfer>
{
    public HttpTransfer FromStore(IDictionary<string, object> values)
    {
        return default;
    }


    public IEnumerable<(string Property, object Value)> ToStore(HttpTransfer entity)
    {
        return null;
    }
}

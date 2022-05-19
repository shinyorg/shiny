using System.Collections.Generic;
using Shiny.Stores.Infrastructure;

namespace Shiny.Net.Http;


public class HttpTransferStoreConverter : IStoreConverter<HttpTransfer>
{
    public HttpTransfer FromStore(IDictionary<string, object> values)
    {
        return default;
    }


    public IEnumerable<(string Property, object value)> ToStore(HttpTransfer entity)
    {
        return null;
    }
}

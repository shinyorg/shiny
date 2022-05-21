using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Notifications.Infrastructure;


public class ChannelStoreConverter : IStoreConverter<Channel>
{
    public Channel FromStore(IDictionary<string, object> values) => throw new System.NotImplementedException();
    public IEnumerable<(string Property, object value)> ToStore(Channel entity) => throw new System.NotImplementedException();
}

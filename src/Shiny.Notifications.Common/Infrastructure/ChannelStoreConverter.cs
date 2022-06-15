using System.Collections.Generic;
using System.Text.Json;
using Shiny.Stores;

namespace Shiny.Notifications.Infrastructure;


public class ChannelStoreConverter : StoreConverter<Channel>
{
    public override Channel FromStore(IDictionary<string, object> values)
    {
        var channel = new Channel
        {
            Identifier = (string)values[nameof(Channel.Identifier)],
            Description = this.ConvertFromStoreValue<string>(values, nameof(Channel.Description)),
            CustomSoundPath = this.ConvertFromStoreValue<string>(values, nameof(Channel.CustomSoundPath)),
            Importance = (ChannelImportance)(long)values[nameof(Channel.Importance)]
        };
        
        if (values.ContainsKey(nameof(Channel.Actions)))
            channel.Actions = JsonSerializer.Deserialize<List<ChannelAction>>((string)values[nameof(channel.Actions)])!;

        return channel;
    }

    public override IEnumerable<(string Property, object Value)> ToStore(Channel entity)
    {
        yield return (nameof(entity.Importance), entity.Importance);

        if (entity.CustomSoundPath != null)
            yield return (nameof(entity.CustomSoundPath), entity.CustomSoundPath);

        if (entity.Description != null)
            yield return (nameof(entity.Description), entity.Description);

        if (entity.Actions.Count > 0)
            yield return (nameof(entity.Actions), JsonSerializer.Serialize(entity.Actions));
    }
}

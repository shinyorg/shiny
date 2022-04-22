using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class ChannelManager : IChannelManager
    {
        readonly IRepository repository;
        public ChannelManager(IRepository repository) => this.repository = repository;


        public Task Add(Channel channel) => this.repository.Set(channel.Identifier, channel);
        public async Task Clear()
        {
            await this.repository.Clear<Channel>().ConfigureAwait(false);
            await this.Add(Channel.Default).ConfigureAwait(false);
        }


        public Task<Channel?> Get(string channelId) => this.repository.Get<Channel>(channelId);
        public Task<IList<Channel>> GetAll() => this.repository.GetList<Channel>();
        public async Task Remove(string channelId)
        {
            this.AssertChannelRemove(channelId);
            await this.repository.Remove<Channel>(channelId).ConfigureAwait(false);
        }
    }
}

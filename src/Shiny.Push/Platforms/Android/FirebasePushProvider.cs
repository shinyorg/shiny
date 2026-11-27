using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Gms.Extensions;
using Firebase.Messaging;
using Shiny.Extensions.Stores;

namespace Shiny.Push;


public class FirebasePushProvider : IPushProvider, IPushTagSupport
{
    const string TagsKey = "Shiny.Push.FirebasePushProvider.RegisteredTags";
    readonly IKeyValueStore? store;
    string[]? registeredTags;


    public FirebasePushProvider() { }


    /// <summary>
    /// Keeps the subscribed topics in <paramref name="store"/>, so ClearTags/SetTags can unsubscribe them after an app restart
    /// </summary>
    public FirebasePushProvider(IKeyValueStore store)
    {
        this.store = store;
        this.registeredTags = store.Get<string[]>(TagsKey);
    }


    public string[]? RegisteredTags
    {
        get => this.registeredTags;
        set
        {
            this.registeredTags = value;
            this.store?.SetOrRemove(TagsKey, value);
        }
    }


    public Task<string> Register(string nativeToken) => Task.FromResult(nativeToken);
    public Task UnRegister()
    {
        this.RegisteredTags = null;
        return Task.CompletedTask;
    }


    public async Task AddTag(string tag)
    {
        await FirebaseMessaging
            .Instance
            .SubscribeToTopic(tag)
            .AsAsync()
            .ConfigureAwait(false);

        var tags = this.RegisteredTags?.ToList() ?? new List<string>(1);
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
            this.RegisteredTags = tags.ToArray();
        }
    }


    public async Task RemoveTag(string tag)
    {
        await FirebaseMessaging
            .Instance
            .UnsubscribeFromTopic(tag)
            .AsAsync()
            .ConfigureAwait(false);

        var list = this.RegisteredTags?.ToList() ?? new List<string>(0);
        if (list.Remove(tag))
            this.RegisteredTags = list.ToArray();
    }


    public async Task ClearTags()
    {
        if (this.RegisteredTags != null)
        {
            foreach (var tag in this.RegisteredTags)
            {
                await FirebaseMessaging
                    .Instance
                    .UnsubscribeFromTopic(tag)
                    .AsAsync()
                    .ConfigureAwait(false);
            }
        }
        this.RegisteredTags = null;
    }


    public async Task SetTags(params string[]? tags)
    {
        await this.ClearTags().ConfigureAwait(false);
        if (tags != null)
        {
            foreach (var tag in tags)
                await this.AddTag(tag).ConfigureAwait(false);
        }
    }
}
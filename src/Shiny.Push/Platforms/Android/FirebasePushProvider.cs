using System;

namespace Shiny.Push;


public class FirebasePushProvider : IPushProvider
{
    public FirebasePushProvider()
    {
    }

    //public async Task AddTag(string tag)
    //{
    //    var tags = this.container.RegisteredTags?.ToList() ?? new List<string>(1);
    //    tags.Add(tag);

    //    await FirebaseMessaging.Instance.SubscribeToTopic(tag);
    //    this.container.RegisteredTags = tags.ToArray();
    //}


    //public async Task RemoveTag(string tag)
    //{
    //    var list = this.container.RegisteredTags?.ToList() ?? new List<string>(0);
    //    if (list.Remove(tag))
    //        this.container.RegisteredTags = list.ToArray();

    //    await FirebaseMessaging
    //        .Instance
    //        .UnsubscribeFromTopic(tag)
    //        .AsAsync()
    //        .ConfigureAwait(false);
    //}


    //public async Task ClearTags()
    //{
    //    if (this.container.RegisteredTags != null)
    //    {
    //        foreach (var tag in this.container.RegisteredTags)
    //        {
    //            await FirebaseMessaging
    //                .Instance
    //                .UnsubscribeFromTopic(tag)
    //                .AsAsync()
    //                .ConfigureAwait(false);
    //        }
    //    }
    //    this.container.RegisteredTags = null;
    //}


    //public async Task SetTags(params string[]? tags)
    //{
    //    await this.ClearTags().ConfigureAwait(false);
    //    if (tags != null)
    //    {
    //        foreach (var tag in tags)
    //            await this.AddTag(tag).ConfigureAwait(false);
    //    }
    //}
}